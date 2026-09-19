// <copyright file="ProgressForm.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

#pragma warning disable CA1416 // This project is compiled for windows
#pragma warning disable VSTHRD111 // The continuations must run on the UI thread of the form.
namespace MUnique.OpenMU.ClientLauncher;

using System.Threading;
using System.Windows.Forms;

/// <summary>
/// Shows the progress of an update process and allows the user to cancel it.
/// </summary>
internal sealed class ProgressForm : Form
{
    private readonly Func<IProgress<UpdateProgress>, CancellationToken, Task> _operation;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Label _messageLabel;
    private readonly ProgressBar _progressBar;
    private readonly Button _cancelButton;
    private bool _isOperationFinished;

    private ProgressForm(string title, Func<IProgress<UpdateProgress>, CancellationToken, Task> operation)
    {
        this._operation = operation;
        this.Text = title;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MinimizeBox = false;
        this.MaximizeBox = false;
        this.ControlBox = false;
        this.ClientSize = new System.Drawing.Size(520, 118);

        this._messageLabel = new Label
        {
            AutoSize = false,
            Location = new System.Drawing.Point(14, 16),
            Size = new System.Drawing.Size(492, 40),
            Text = "Preparing...",
        };

        this._progressBar = new ProgressBar
        {
            Location = new System.Drawing.Point(14, 60),
            Size = new System.Drawing.Size(492, 20),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
        };

        this._cancelButton = new Button
        {
            Location = new System.Drawing.Point(431, 86),
            Size = new System.Drawing.Size(75, 26),
            Text = "Cancel",
            UseVisualStyleBackColor = true,
        };
        this._cancelButton.Click += this.OnCancelClick;

        this.Controls.Add(this._messageLabel);
        this.Controls.Add(this._progressBar);
        this.Controls.Add(this._cancelButton);
        this.Shown += this.OnShown;
        this.FormClosing += this.OnFormClosing;
    }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    internal bool Succeeded { get; private set; }

    /// <summary>
    /// Gets the error message of the operation, if there was one.
    /// </summary>
    internal string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user canceled the operation.
    /// </summary>
    internal bool Canceled { get; private set; }

    /// <summary>
    /// Runs the specified operation while showing a progress dialog.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="operation">The operation to run.</param>
    /// <returns>The result of the operation.</returns>
    internal static ProgressResult Run(
        IWin32Window owner,
        string title,
        Func<IProgress<UpdateProgress>, CancellationToken, Task> operation)
    {
        using var form = new ProgressForm(title, operation);
        form.ShowDialog(owner);
        return new ProgressResult(form.Succeeded, form.Canceled, form.ErrorMessage);
    }

    private void OnShown(object? sender, EventArgs e)
    {
#pragma warning disable VSTHRD110 // The task is awaited inside the dialog; the dialog closes when it finishes.
        _ = this.RunOperationAsync();
#pragma warning restore VSTHRD110
    }

    private async Task RunOperationAsync()
    {
        var progress = new Progress<UpdateProgress>(this.OnProgress);
        try
        {
            await this._operation(progress, this._cancellationTokenSource.Token);
            this.Succeeded = true;
        }
        catch (OperationCanceledException)
        {
            this.Canceled = true;
            this.ErrorMessage = "The update was canceled.";
            LauncherLog.Warn("The update was canceled by the user.");
        }
        catch (Exception ex)
        {
            this.ErrorMessage = ex.Message;
            LauncherLog.Error("The update failed.", ex);
        }
        finally
        {
            this._isOperationFinished = true;
            this.Close();
        }
    }

    private void OnProgress(UpdateProgress progress)
    {
        this._messageLabel.Text = progress.Message;
        if (progress.IsIndeterminate)
        {
            this._progressBar.Style = ProgressBarStyle.Marquee;
        }
        else
        {
            this._progressBar.Style = ProgressBarStyle.Continuous;
            this._progressBar.Value = (int)Math.Clamp(progress.Fraction * 100, 0, 100);
        }
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        this._cancelButton.Enabled = false;
        this._messageLabel.Text = "Canceling...";
        this._cancellationTokenSource.Cancel();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._cancellationTokenSource.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // The dialog closes itself when the operation has finished; a close request of the
        // user is treated as a cancellation of the running operation.
        if (e.CloseReason == CloseReason.UserClosing && !this._isOperationFinished)
        {
            e.Cancel = true;
            this.OnCancelClick(sender, EventArgs.Empty);
        }
    }
}
