// <copyright file="TestHttpServer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ClientLauncher.Tests;

using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// A minimal http server which serves byte arrays and supports range requests.
/// </summary>
internal sealed class TestHttpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _requestCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Task> _clientTasks = [];
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _acceptTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestHttpServer"/> class.
    /// </summary>
    internal TestHttpServer()
    {
        this._listener = new TcpListener(IPAddress.Loopback, 0);
        this._listener.Start();
        var port = ((IPEndPoint)this._listener.LocalEndpoint).Port;
        this.BaseUrl = string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port}/");
        this._acceptTask = Task.Run(this.AcceptLoopAsync);
    }

    /// <summary>
    /// Gets the base url of the server.
    /// </summary>
    internal string BaseUrl { get; }

    /// <summary>
    /// Gets or sets the number of bytes after which the response is aborted, to simulate a broken connection.
    /// </summary>
    internal int? AbortAfterBytes { get; set; }

    /// <summary>
    /// Adds a file which can be downloaded from the server.
    /// </summary>
    /// <param name="path">The path of the file, e.g. <c>/manifest.json</c>.</param>
    /// <param name="content">The content.</param>
    internal void AddFile(string path, byte[] content) => this._files[path] = content;

    /// <summary>
    /// Adds a file which can be downloaded from the server.
    /// </summary>
    /// <param name="path">The path of the file, e.g. <c>/manifest.json</c>.</param>
    /// <param name="content">The content.</param>
    internal void AddFile(string path, string content) => this.AddFile(path, Encoding.UTF8.GetBytes(content));

    /// <summary>
    /// Gets how often the specified path has been requested.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The number of requests.</returns>
    internal int GetRequestCount(string path) => this._requestCounts.TryGetValue(path, out var count) ? count : 0;

    /// <inheritdoc />
    public void Dispose()
    {
        this._cancellationTokenSource.Cancel();
        this._listener.Stop();
        try
        {
            Task.WaitAll([.. this._clientTasks, this._acceptTask], TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // The tasks are canceled.
        }

        this._cancellationTokenSource.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        while (!this._cancellationTokenSource.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await this._listener.AcceptTcpClientAsync(this._cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            this._clientTasks.Add(Task.Run(() => this.HandleClientAsync(client)));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var requestLine = await ReadLineAsync(stream).ConfigureAwait(false);
                if (requestLine is null)
                {
                    return;
                }

                long rangeStart = 0;
                string? line;
                while ((line = await ReadLineAsync(stream).ConfigureAwait(false)) is { Length: > 0 })
                {
                    if (line.StartsWith("Range:", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = line["Range:".Length..].Trim();
                        var startText = value.Replace("bytes=", string.Empty, StringComparison.OrdinalIgnoreCase).Split('-')[0];
                        _ = long.TryParse(startText, NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeStart);
                    }
                }

                var path = requestLine.Split(' ')[1];
                this._requestCounts[path] = this.GetRequestCount(path) + 1;
                if (!this._files.TryGetValue(path, out var content))
                {
                    await WriteResponseAsync(stream, "404 Not Found", [], []).ConfigureAwait(false);
                    return;
                }

                if (rangeStart > 0 && rangeStart < content.Length)
                {
                    var partial = content[(int)rangeStart..];
                    var headers = new[]
                    {
                        $"Content-Range: bytes {rangeStart}-{content.Length - 1}/{content.Length}",
                    };
                    await WriteResponseAsync(stream, "206 Partial Content", headers, partial).ConfigureAwait(false);
                    return;
                }

                if (this.AbortAfterBytes is { } abortAfter && abortAfter < content.Length)
                {
                    var headers = new[]
                    {
                        $"Content-Length: {content.Length}",
                    };
                    var headerText = $"HTTP/1.1 200 OK\r\n{string.Join("\r\n", headers)}\r\n\r\n";
                    var headerBytes = Encoding.ASCII.GetBytes(headerText);
                    await stream.WriteAsync(headerBytes).ConfigureAwait(false);
                    await stream.WriteAsync(content.AsMemory(0, abortAfter)).ConfigureAwait(false);
                    await stream.FlushAsync().ConfigureAwait(false);
                    this.AbortAfterBytes = null;
                    client.Client.Close(0);
                    return;
                }

                await WriteResponseAsync(stream, "200 OK", [], content).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // The client closed the connection.
            }
            catch (ObjectDisposedException)
            {
                // The server has been stopped.
            }
        }
    }

    private static async Task<string?> ReadLineAsync(NetworkStream stream)
    {
        var buffer = new byte[1];
        var builder = new StringBuilder();
        while (true)
        {
            var read = await stream.ReadAsync(buffer).ConfigureAwait(false);
            if (read == 0)
            {
                return builder.Length > 0 ? builder.ToString() : null;
            }

            if (buffer[0] == (byte)'\n')
            {
                return builder.ToString().TrimEnd('\r');
            }

            builder.Append((char)buffer[0]);
        }
    }

    private static async Task WriteResponseAsync(NetworkStream stream, string status, string[] additionalHeaders, byte[] content)
    {
        var headers = new List<string>
        {
            $"Content-Length: {content.Length}",
            "Content-Type: application/octet-stream",
            "Connection: close",
        };
        headers.AddRange(additionalHeaders);
        var headerText = $"HTTP/1.1 {status}\r\n{string.Join("\r\n", headers)}\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(headerText)).ConfigureAwait(false);
        await stream.WriteAsync(content).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }
}
