# Thiết kế Launcher và Updater cho client (máy chủ x9999)

Trạng thái: **đã triển khai giai đoạn P1–P3**, xem mục 14 để biết phần còn lại.

Phạm vi: launcher Windows tự cập nhật client từ xa rồi khởi động game.

Máy chủ: OpenMU chạy trên máy Lenovo, người chơi kết nối qua Tailscale.
Repo: `x9999mu/OpenMU` (launcher, máy chủ) và `x9999mu/MuMain` (client), cả hai public.

## 1. Mục tiêu

1. Người chơi tải **một file launcher nhỏ**, mở lên là có client đầy đủ để chơi.
2. Lần chạy đầu tiên: tải và cài toàn bộ client, không cần thao tác thủ công.
3. Các lần sau: chỉ tải những **archive** thực sự thay đổi (runtime và/hoặc data),
   verify SHA-256, rồi mới start game.
4. Cập nhật thất bại hoặc mất mạng không làm hỏng bản cài hiện có, không chặn người chơi
   đang chơi được.
5. Không đụng vào `config.ini` (chứa setting và credential đã lưu của người chơi).

## 2. Ngoài phạm vi

- Không chống gian lận, không kiểm tra tính toàn vẹn file phía client.
- Không chặn client cũ ở phía máy chủ (theo yêu cầu vận hành).
- **Không làm delta từng file.** Khi data đổi, người chơi tải lại toàn bộ archive data
  (~447 MB). Đây là lựa chọn có chủ đích để đổi lấy thiết kế đơn giản nhất: không cần
  nginx phục vụ file, không cần sinh manifest theo từng file, không cần cache hash
  13.170 file trên máy người chơi.
- Không dùng luồng patch FTP `C1 02` / `ClientNeedsPatch` có sẵn của OpenMU.
  Luồng này hiện không hoạt động với client MuMain vì client không gọi
  `SendPatchCheckRequest`; giữ nguyên, không mở rộng.
- Không làm launcher cho macOS/Linux ở giai đoạn này (chỉ Windows).
- Không viết bản launcher native C++ ở giai đoạn này (xem mục 15).

## 3. Ràng buộc và hiện trạng

### 3.1 Hạ tầng máy chủ

- Game server và admin panel chạy bằng Docker Compose tại `deploy/all-in-one/`.
- nginx đã có sẵn trong `deploy/all-in-one/nginx/`, phục vụ 80/443 và proxy `/`
  về `openmu-startup:8080`. Cổng game `44405` được publish ra host.
- Máy Lenovo nằm trong tailnet; hướng dẫn hiện tại đưa ra hai địa chỉ:
  LAN `192.168.1.28` và Tailscale `100.108.169.118`, cổng `44405`.
- Deploy tự động qua `deploy/all-in-one/deploy-lenovo.sh`, dùng image `ghcr.io/x9999mu/openmu`.
- **Máy Lenovo không cần phục vụ file cập nhật.** Toàn bộ việc phân phối file đi qua GitHub.

### 3.2 Artifact client đã có sẵn trên GitHub

Repo `x9999mu/MuMain` đã publish đủ nguyên liệu để làm updater:

| Nguồn | Nội dung | Ghi chú |
| --- | --- | --- |
| release `v<version>` (semantic-release từ `main`) | `MuMain-windows-native-x64-release-editor-off-no-data.tar.gz` | runtime, không kèm data, tên asset cố định; có `config.ini` bên trong |
| release `data-<id>` | `MuMain-data-<id>.tar.gz` + `.sha256` | `<id>` là hash của cây `Data/` + `fonts/`, tạo bởi `data-assets.yml`, đánh dấu `--latest=false` |
| release `develop-latest` (prerelease) | zip kèm data, `config.ini` được bake sẵn IP server develop | dùng cho nhánh `develop` |

Điểm quan trọng: dữ liệu đã được **content-addressed theo cả cây thư mục**. Chỉ cần đổi
một file `.bmd` là `<id>` đổi và có archive mới. Vì asset là một file nén duy nhất, không
thể tải lẻ từng file từ GitHub — đây chính là lý do của ràng buộc ở mục 2.

Runtime archive và data archive ghép lại thành một bản client hoàn chỉnh:
`Main.exe`, `MUnique.Client.Library.dll`, DLL runtime (MSVC CRT, SDL3, SDL3_mixer),
`shaders/`, `config.ini` nằm ở archive runtime; `Data/` và `fonts/` nằm ở archive data.

### 3.3 Launcher hiện tại

`src/ClientLauncher/` là ứng dụng WinForms `net10.0-windows`, đã có:

- `Launcher.cs`: ghi registry `HKLM\SOFTWARE\WebZen\Mu\Connection` (ParameterA/ParameterB
  đã mã hoá) rồi chạy `Main.exe connect /u<ip> /p<port>`.
- `LauncherSettings.cs`: `MainExePath`, danh sách host, danh sách resolution.
- `ClientSettings.cs`: đọc/ghi `HKCU\SOFTWARE\WebZen\Mu\Config`.
- UI: `MainForm`, `HostConfigurationDialog`, `ClientSettingsDialog`.

Launcher này **chưa có bất kỳ logic cập nhật nào**. Phần registry chỉ cần cho client
đóng kín ngày xưa; client MuMain đọc `config.ini` và nhận `/u` `/p` từ command line
(`client/src/source/App/Platform/Windows/Winmain.cpp`), nên đường đi mới là command line.

### 3.4 Kích thước dữ liệu client

Đo trên `data-download/Data` và `data-download/fonts` (bản tương ứng
`MuMain-data-7fce146eb3fe34a0.tar.gz`):

| Chỉ số | Giá trị |
| --- | --- |
| Số file | 13.170 (Data 13.160, fonts 10) |
| Tổng dung lượng giải nén | 710,4 MB |
| Archive tar.gz | ~447 MB |
| Runtime không data | ~16 MB |
| File lớn nhất | 4 MB |
| File ≥ 1 MB | 23 file (86,9 MB) |
| File ≥ 256 KB | 395 file (205,7 MB) |

Nhóm lớn nhất trong `Data/`: `Monster` 947 file/82,9 MB, `Interface` 761/36,2,
`Player` 1.031/33,8, `Item` 1.276/27,5, `Local` 268/21,8, `NPC` 335/21,7.

## 4. Quyết định thiết kế

| ID | Quyết định | Lý do |
| --- | --- | --- |
| D1 | Manifest JSON trên GitHub Releases là nguồn chân lý | Repo public, CDN sẵn, không cần hạ tầng riêng |
| D2 | Mọi thứ tải từ GitHub Releases: manifest, runtime archive, data archive, launcher | Đơn giản nhất; cập nhật không cần Tailscale |
| D3 | Cài lần đầu lấy cả runtime + data archive | Một lần cho mỗi máy |
| D4 | Khi data đổi: tải lại toàn bộ archive data (~447 MB) | Chấp nhận (phương án C); tránh toàn bộ phức tạp của delta |
| D5 | Không đụng `config.ini`; truyền `/u` `/p` khi start | Giữ credential và setting của người chơi |
| D6 | Cài mặc định vào `%LOCALAPPDATA%\MuOnline` | Không cần quyền admin, tránh antivirus gắt |
| D7 | Verify SHA-256 ở mức archive, không hash từng file | Đủ an toàn, nhanh, không cần cache hash |
| D8 | Cập nhật lỗi thì vẫn cho Start kèm cảnh báo | Yêu cầu vận hành: không chặn client cũ |
| D9 | Không xoá file khi cập nhật; có nút "Cài lại sạch" | Tránh vô tình xoá dữ liệu người chơi |
| D10 | Launcher giữ C#, publish self-contained single-file | Tận dụng code có sẵn, triển khai nhanh nhất |

## 5. Kiến trúc

### 5.1 Thành phần

```
Người chơi (Windows)                    GitHub (public)                     Máy Lenovo (tailnet)
┌────────────────────┐   manifest      ┌──────────────────────────────┐
│ MuMainLauncher.exe │ ──────────────► │ release client-latest        │
│  - đọc manifest    │                 │  manifest.json               │
│  - tải archive     │   archives      └──────────────────────────────┘
│  - verify + cài    │ ──────────────► ┌──────────────────────────────┐
│  - start game      │                 │ release v<version>           │
└────────────────────┘                 │  runtime tar.gz (~16 MB)     │
         │                             └──────────────────────────────┘
         │                             ┌──────────────────────────────┐
         │                             │ release data-<id>            │
         │                             │  data tar.gz (~447 MB)       │
         │                             └──────────────────────────────┘
         │
         └─────────► Main.exe connect /u… /p44405 ────► openmu-startup:44405
                     (kết nối game qua Tailscale hoặc LAN)
```

Launcher không cần Tailscale để cập nhật; chỉ cần Tailscale (hoặc LAN) để vào game.

### 5.2 Thứ tự ưu tiên nguồn

| Loại | Nguồn | Fallback |
| --- | --- | --- |
| `manifest.json` | `github.com/x9999mu/MuMain/releases/download/client-latest/manifest.json` | không có |
| Runtime archive | asset của release `v<version>` | không có |
| Data archive | asset của release `data-<id>` | không có |
| `MuMainLauncher.exe` | asset của release `launcher-v<version>` trong repo OpenMU | không có |

Một nguồn duy nhất giúp luồng cập nhật dễ hiểu và dễ chẩn đoán. Nếu sau này GitHub bị
chặn hoặc cần dự phòng, việc thêm một nguồn gương chỉ là thêm URL vào manifest (mục 15).

### 5.3 `manifest.json` — schema v1

```json
{
  "schemaVersion": 1,
  "channel": "stable",
  "generatedAtUtc": "2026-09-19T02:00:00Z",
  "runtime": {
    "version": "1.4.2",
    "tag": "v1.4.2",
    "archive": {
      "url": "https://github.com/x9999mu/MuMain/releases/download/v1.4.2/MuMain-windows-native-x64-release-editor-off-no-data.tar.gz",
      "size": 16777216,
      "sha256": "…",
      "format": "tar.gz"
    }
  },
  "data": {
    "id": "7fce146eb3fe34a0",
    "tag": "data-7fce146eb3fe34a0",
    "archive": {
      "url": "https://github.com/x9999mu/MuMain/releases/download/data-7fce146eb3fe34a0/MuMain-data-7fce146eb3fe34a0.tar.gz",
      "size": 468713472,
      "sha256": "…",
      "format": "tar.gz"
    }
  },
  "launcher": {
    "version": "1.1.0",
    "url": "https://github.com/x9999mu/OpenMU/releases/download/launcher-v1.1.0/MuMainLauncher.exe",
    "size": 41943040,
    "sha256": "…"
  },
  "server": {
    "host": "100.108.169.118",
    "hostName": "lenovo.tailnet.ts.net",
    "port": 44405
  },
  "preserve": ["config.ini"]
}
```

Ghi chú:

- Manifest chỉ vài KB: không liệt kê từng file, không có hash từng file.
- `runtime.version` quyết định có tải archive runtime hay không.
- `data.id` quyết định có tải archive data hay không.
- `preserve` là danh sách đường dẫn launcher **không bao giờ ghi đè** khi giải nén.
- `server` cho phép đổi IP/cổng tập trung: người chơi không cần sửa gì, launcher tự lấy.
- `sha256` của archive do CI tính tại chỗ (ngay khi build xong) rồi ghi vào manifest,
  nên không cần tải thêm file `.sha256` khi cập nhật.

### 5.4 Layout trên GitHub

| Repo | Tag | Asset |
| --- | --- | --- |
| `x9999mu/MuMain` | `v<version>` | runtime tar.gz 3 nền tảng (tuỳ chọn, có thì manifest dùng luôn) |
| `x9999mu/MuMain` | `data-<id>` | data tar.gz + `.sha256` (đã có) |
| `x9999mu/MuMain` | `client-latest` | `manifest.json` (mới, cập nhật bằng `--clobber`) |
| `x9999mu/OpenMU` | `launcher-v<version>` | `MuMainLauncher.exe`, `.sha256` (mới) |

`client-latest` là tag cố định nên URL manifest không bao giờ đổi. Cách này giống pattern
`develop-latest` mà repo client đang dùng. Release data đã được đánh dấu `--latest=false`,
nên `releases/latest` vẫn luôn trỏ về bản runtime mới nhất.

Repo client đang phát triển trên nhánh `develop` và nhánh đó **không** tạo release `v<version>`,
nên workflow lấy runtime theo hai đường:

1. Nếu có release `v<version>` chứa asset runtime → dùng asset đó, `runtime.version` là số version.
2. Nếu không (trường hợp thường gặp trên `develop`) → chờ workflow `ci.yml` của đúng commit đó
   build xong rồi tải artifact `mu-client-windows-native-x64-release-editor-off-no-data-main`.
   `runtime.version` khi đó là `develop-<12 ký tự đầu của sha256>`, nên commit không đổi runtime
   sẽ không bắt người chơi tải lại.

Data vẫn luôn lấy từ release `data-<id>`. Vì `data-assets.yml` chỉ chạy khi `Data/`/`fonts/` đổi,
workflow manifest sẽ tự dispatch `data-assets.yml` nếu release tương ứng chưa tồn tại.

Version của launcher lấy từ assembly (`src/SharedAssemblyInfo.cs`), nên tag release cũng
dùng đúng version đó (`launcher-v0.9.10.0`). Muốn phát hành launcher mới thì bump version
của repo (ví dụ bằng `set-projectVersion.ps1`) trước khi chạy workflow.

### 5.5 Máy Lenovo

Không cần thay đổi gì cho việc phân phối file:

- Không thêm `location` mới vào nginx.
- Không cần thư mục static, không cần script đồng bộ, không tốn dung lượng đĩa cho archive.
- Băng thông tải client đi qua CDN của GitHub, không đi qua đường upload của nhà.

Máy Lenovo chỉ tiếp tục làm hai việc như hiện tại: chạy game server và nằm trong tailnet.

## 6. Trạng thái trên máy người chơi

### 6.1 Cây thư mục

```
%LOCALAPPDATA%\MuOnline\
├── client\                     thư mục cài game (Main.exe, Data\, fonts\, shaders\, …)
├── launcher-state.json         trạng thái đã cài/verify
├── launcher.log                log xoay vòng
├── .staging\                   nơi tải và giải nén tạm
├── .cache\                     archive đã verify, giữ để cài lại nhanh
└── .backup\<timestamp>\        chỉ dùng cho "Cài lại sạch"
```

`.cache\` giữ archive của bản đang dùng và bản runtime trước đó (nhỏ, ~16 MB);
archive data cũ bị xoá khi cập nhật để không chiếm thêm 447 MB mỗi phiên bản.
Tổng dung lượng cache giới hạn khoảng 1,2 GB.

### 6.2 `launcher-state.json`

```json
{
  "schemaVersion": 1,
  "channel": "stable",
  "installDir": "C:\\Users\\me\\AppData\\Local\\MuOnline\\client",
  "installed": { "runtimeVersion": "1.4.2", "dataId": "7fce146eb3fe34a0" },
  "installedAtUtc": "2026-09-19T02:05:00Z",
  "launcherVersion": "1.1.0",
  "lastLaunchUtc": "2026-09-19T02:07:00Z"
}
```

### 6.3 Vì sao không cần cache hash từng file

Ta chỉ hash **archive** (một lần cho 447 MB, tốc độ vài trăm MB/s trên SSD) rồi giải nén.
Không cần biết từng file có đổi hay không, nên:

- Manifest nhỏ và sinh trong CI rất đơn giản (chỉ cần hash 2 archive).
- Launcher không phải quét 710 MB / 13.170 file mỗi lần mở.
- Không có trạng thái cache phức tạp để hỏng hay phải migrate.

Đánh đổi: khi data đổi, người chơi tải lại 447 MB (quyết định D4).

## 7. Luồng hoạt động

### 7.1 Lần chạy đầu tiên (cài đặt)

1. Đọc cấu hình launcher (`launcher.json` cạnh exe) hoặc dùng mặc định; cho chọn thư mục cài
   (mặc định `%LOCALAPPDATA%\MuOnline\client`).
2. Tải `manifest.json` từ `client-latest`.
3. Kiểm tra dung lượng trống ≥ 1,2 GB trước khi tải.
4. Tải runtime archive + data archive vào `.staging\`, có resume bằng HTTP Range.
5. Verify SHA-256 từng archive; archive đã verify được giữ lại trong `.cache\`.
6. Giải nén lần lượt vào `.staging\payload\` (chặn path traversal, absolute path, symlink).
7. Copy từ `.staging\payload\` vào thư mục cài, **bỏ qua** đường dẫn nằm trong `preserve`
   nếu file đã tồn tại.
8. Ghi `launcher-state.json`.
9. Start game.

### 7.2 Các lần chạy sau (cập nhật)

1. Tải `manifest.json`.
2. So sánh: `runtime.version` và `data.id` khớp state thì bỏ qua (fast path).
3. Ngược lại, xác định archive cần tải:
   - runtime khác → tải runtime archive (~16 MB);
   - data id khác → tải data archive (~447 MB);
   - có thể phải tải cả hai.
4. Tải vào `.staging\`, resume nếu đứt, verify SHA-256.
5. Kiểm tra game chưa chạy (xem 8.5).
6. Giải nén và copy vào thư mục cài như bước 6–7 ở trên (không xoá file cũ).
7. Cập nhật `launcher-state.json`, dọn archive data cũ trong `.cache\`.
8. Start game.

### 7.3 Khởi động game

- Luôn truyền tham số: `Main.exe connect /u<host> /p<port>` với host/cổng lấy từ manifest,
  có thể override trong dialog cấu hình host của launcher.
- Nếu phân giải được hostname Tailscale thì dùng địa chỉ IPv4 như launcher hiện tại.
  Phần ghi registry (`HostEncode`/`PortEncode`) chỉ dùng khi người dùng bật tuỳ chọn
  "client cũ cần registry".
- `config.ini` không bị sửa.

### 7.4 Khi mất mạng

- Không tải được manifest: hiện thông báo và cho Start với bản hiện có (D8).
- Đang tải archive mà đứt: giữ `.part` để lần sau resume; không ghi gì vào thư mục cài
  cho tới khi verify xong.
- Nếu chưa từng cài đặt gì thì không thể chơi; launcher hiển thị hướng dẫn kiểm tra mạng.

### 7.5 Tự cập nhật launcher

1. So `launcher.version` trong manifest với version của chính nó.
2. Nếu mới hơn: tải về `MuMainLauncher.new.exe`, verify SHA-256.
3. Chạy `MuMainLauncher.new.exe --apply-self-update --target <đường dẫn exe cũ> --wait-pid <pid>`.
4. Tiến trình mới chờ tiến trình cũ thoát (tối đa 60 giây), copy đè lên exe cũ, rồi tiếp tục
   luồng khởi động bình thường.

Windows khoá file exe đang chạy nên bắt buộc phải qua tiến trình trung gian này.

## 8. Thuật toán chi tiết

### 8.1 Fast path

Nếu `state.installed.runtimeVersion == manifest.runtime.version` và
`state.installed.dataId == manifest.data.id` thì chỉ kiểm tra `Main.exe` tồn tại và
kích thước khớp với entry trong archive đã lưu ở `.cache\` (nếu còn). Trường hợp này mở
launcher gần như tức thì và không tải gì.

### 8.2 Chọn archive cần tải

```
needRuntime = state.runtimeVersion != manifest.runtime.version
needData    = state.dataId         != manifest.data.id
```

- Lần đầu (không có state): cả hai đều `true`.
- Release chỉ đổi code client: chỉ `needRuntime`.
- Data đổi: `needData` (kèm `needRuntime` nếu release runtime cũng mới).

### 8.3 Tải và resume

- Ghi ra `<tên>.part`, dùng `Range: bytes=<done>-` khi server trả `206`.
- Server không hỗ trợ Range thì tải lại từ đầu.
- Retry 3 lần với backoff cho lỗi mạng/5xx; hiển thị MB/s và thời gian còn lại.
- Kiểm tra `Content-Length` khớp `archive.size` trước khi verify SHA-256.
- Không cần tải file `.sha256` riêng vì hash đã có trong manifest.

### 8.4 Giải nén an toàn

- Dùng `System.Formats.Tar` + `GZipStream` (có sẵn trong .NET 10), không cần thư viện ngoài.
- Từ chối entry có đường dẫn tuyệt đối, chứa `..`, hoặc là symlink/hardlink.
- Chỉ extract vào `.staging\payload\`.

### 8.5 Phát hiện game đang chạy và apply

- Thử mở `Main.exe` với quyền ghi: nếu bị chia sẻ khoá (sharing violation) thì game đang chạy.
- Nếu đang chạy: hỏi người chơi đóng game, không tự kill tiến trình.
- Copy từng file từ staging vào thư mục cài: ghi ra file tạm cùng thư mục rồi `File.Replace`.
- Bỏ qua file nằm trong `preserve` nếu đã tồn tại ở thư mục cài.
- **Không xoá file** không còn trong archive (D9). Nếu việc này gây lỗi về sau, người chơi
  dùng nút "Cài lại sạch": launcher chuyển thư mục client vào `.backup\<timestamp>\`
  (giữ lại `config.ini`), rồi cài mới từ archive trong `.cache\`.
- Nếu apply lỗi giữa chừng: chạy lại bước apply từ archive đã verify (không cần tải lại
  trừ khi archive đã bị dọn), giữ state cũ, báo lỗi.

### 8.6 Rollback

- Archive runtime của bản trước được giữ trong `.cache\` nên có thể quay lại bản cũ ngay.
- Archive data cũ không được giữ (447 MB); muốn quay lại thì launcher tải lại theo
  `data.id` cũ nếu còn trên GitHub, hoặc dùng "Cài lại sạch" với bản mới nhất.
- State cũ được sao lưu thành `launcher-state.json.bak` trước mỗi lần ghi.

## 9. Bảo mật

- Verify SHA-256 cho mọi archive tải về; sai hash thì tải lại, quá 3 lần thì dừng và báo lỗi.
- Chỉ tải qua HTTPS từ GitHub.
- Không chạy file tải về ngoài hai đường dẫn cố định: `Main.exe` trong thư mục cài và
  `MuMainLauncher.exe` trong bước self-update.
- Không lưu token hay bí mật trong manifest; `host`/`port` không phải bí mật, quyền truy cập
  game do tailnet quyết định.
- Không yêu cầu quyền admin; không ghi vào `Program Files`, không sửa registry
  (trừ tuỳ chọn dành cho client cũ).
- Chống path traversal khi giải nén (mục 8.4) và chỉ ghi trong thư mục cài đã chọn.

## 10. Hiệu năng và băng thông

| Tình huống | Lượng tải | Ghi chú |
| --- | --- | --- |
| Cài lần đầu | ~16 MB + ~447 MB | một lần cho mỗi máy, qua CDN GitHub |
| Release mới, data không đổi | ~16 MB | phần lớn các lần cập nhật |
| Data đổi (bất kỳ file nào) | ~447 MB | đánh đổi đã chấp nhận ở D4 |
| Mở launcher, đã cập nhật | vài KB manifest | dưới 1 giây |

Tối ưu đã dùng: một kết nối cho archive lớn (có resume), giải nén tuần tự để không tốn
RAM, hash streaming để không nạp cả file vào bộ nhớ. Máy chủ game không tham gia vào việc
phân phối nên không ảnh hưởng băng thông khi nhiều người cài cùng lúc.

## 11. Log và chẩn đoán

- `launcher.log` ghi: version launcher, URL manifest, HTTP status, archive nào được tải,
  hash mismatch, lỗi IO, thời gian từng bước, kết quả apply.
- Chế độ `--diagnostics`: kiểm tra kết nối GitHub, quyền ghi thư mục cài, dung lượng trống,
  version .NET, trạng thái state; in kết quả để gửi cho admin.

## 12. Thay đổi cần làm trong repo

### 12.1 `x9999mu/OpenMU` — launcher

Thêm mới trong `src/ClientLauncher/Updater/`:

- `UpdateManifest.cs` — model + parse/validate schema v1.
- `ManifestClient.cs` — `HttpClient` tải manifest, retry 3 lần với backoff.
- `FileHasher.cs` — SHA-256 streaming cho archive.
- `Downloader.cs` — tải có resume, verify, báo tiến độ.
- `ArchiveExtractor.cs` — tar.gz, chặn path traversal, hỗ trợ danh sách `preserve`.
- `InstallState.cs` — đọc/ghi `launcher-state.json` (kèm file `.bak`).
- `UpdateService.cs` — điều phối cài đặt/cập nhật/cài lại sạch.
- `SelfUpdater.cs` — chế độ `--apply-self-update`.
- `ProgressForm.cs` — UI tiến trình, huỷ, thông báo lỗi và lựa chọn khi lỗi.

Sửa: `MainForm.cs` (nút Kiểm tra/Cập nhật/Start/Cài lại sạch), `Launcher.cs` (ưu tiên command
line, registry chỉ là tuỳ chọn), `LauncherSettings.cs` (thêm `installDir`, `channel`, `manifestUrl`).

Thêm mới: `.github/workflows/launcher-release.yml` — publish self-contained single-file
`win-x64`, tạo release tag `launcher-v<version>` kèm `MuMainLauncher.exe` + `.sha256`.

Thêm mới: `tests/MUnique.OpenMU.ClientLauncher.Tests/` cho các unit test ở mục 13.1.

### 12.2 `x9999mu/MuMain` — sinh manifest

Workflow `client-manifest.yml` sinh `manifest.json`:

- Chạy tự động khi push lên `main`/`develop` và khi có release mới (`v*` hoặc `data-*`);
  có thể chạy tay với input `runtime_tag` khi cần trỏ về một release cụ thể.
- Lấy runtime từ release `v<version>` nếu có, nếu không thì từ artifact của `ci.yml`
  cho đúng commit đang chạy (chờ CI build xong trước khi tải).
- Lấy data từ release `data-<id>`, tự dispatch `data-assets.yml` nếu chưa có.
- Tính `sha256` + `size` của hai archive, điền `runtime.version`, `data.id`, `server`,
  `launcher`, `preserve`.
- Upload vào tag `client-latest` bằng `gh release upload --clobber`.
- Script sinh đặt tại `client/tools/generate_manifest.py`, chạy lại được ở máy local
  (có tuỳ chọn `--base-url` để phục vụ archive từ một http server local).

### 12.3 `deploy/all-in-one`

Không cần thay đổi. Việc phân phối client đi qua GitHub; nginx và máy Lenovo giữ nguyên.

## 13. Kiểm thử

### 13.1 Unit test

- Parse manifest: thiếu field, sai `schemaVersion`, JSON hỏng, URL không phải HTTPS.
- Quyết định cần tải: state rỗng, chỉ runtime đổi, chỉ data đổi, cả hai đổi, không đổi.
- `ArchiveExtractor`: chặn `../`, đường dẫn tuyệt đối, symlink; bỏ qua `preserve` khi file đã tồn tại.
- `Downloader`: resume khi có `.part`, verify sai hash, retry khi server lỗi 5xx.
- `InstallState`: ghi đọc lại, state cũ thiếu field, khôi phục từ `.bak`.

### 13.2 Integration test

Dùng HTTP server cục bộ (fixture manifest + archive nhỏ) để chạy trọn luồng:

- Cài từ trạng thái trống.
- Mở lại khi không có gì đổi → không tải archive nào.
- Chỉ đổi `data.id` → chỉ tải data archive.
- Chỉ đổi `runtime.version` → chỉ tải runtime archive.
- Ngắt kết nối giữa chừng → resume thành công; hash sai → thất bại an toàn, state không đổi.

### 13.3 Ma trận kiểm thử thủ công

| # | Kịch bản | Kỳ vọng |
| --- | --- | --- |
| 1 | Máy trắng, mạng tốt | Cài xong, vào game |
| 2 | Máy trắng, chưa bật Tailscale | Cài xong (qua GitHub), launcher báo cần Tailscale để chơi |
| 3 | Mở launcher khi đã cập nhật | Vào game dưới 2 giây, không tải gì |
| 4 | Release mới runtime, data không đổi | Chỉ tải ~16 MB |
| 5 | Data đổi | Tải ~447 MB rồi vào game |
| 6 | Rút mạng giữa lúc tải | Resume, không hỏng bản cài |
| 7 | Game đang chạy, có bản mới | Không ghi file, hỏi đóng game |
| 8 | `config.ini` đã chỉnh sửa | Không bị ghi đè |
| 9 | Launcher có bản mới | Tự cập nhật rồi tiếp tục |
| 10 | Bấm "Cài lại sạch" | Client mới, `config.ini` được giữ |
| 11 | Thư mục cài có ký tự Unicode/khoảng trắng | Chạy đúng |
| 12 | Ổ đĩa gần hết | Báo lỗi rõ, không ghi dở |
| 13 | Antivirus chặn ghi | Báo lỗi kèm gợi ý, không treo |

### 13.4 Chạy thử bằng GUI tại máy local (không cần GitHub)

Cách này cho đúng trải nghiệm người chơi: mở launcher, thấy thanh tiến trình, rồi Start game.

1. Tạo thư mục phục vụ, ví dụ `D:\mu-test\serve`.
2. Tạo archive runtime từ bản build client (Windows có sẵn `tar`):
   `tar -czf D:\mu-test\serve\MuMain-runtime-test.tar.gz -C <thư-mục-build>\Release .`
   (cần `Main.exe`, `MUnique.Client.Library.dll`, các DLL runtime, `shaders\`, `config.ini`)
3. Chép archive data vào cùng thư mục, ví dụ `MuMain-data-7fce146eb3fe34a0.tar.gz`.
4. Sinh manifest trỏ về server local:

   ```sh
   python client/tools/generate_manifest.py \
     --runtime-version 1.0.0 \
     --runtime-archive D:\mu-test\serve\MuMain-runtime-test.tar.gz \
     --data-id 7fce146eb3fe34a0 \
     --data-archive D:\mu-test\serve\MuMain-data-7fce146eb3fe34a0.tar.gz \
     --base-url http://127.0.0.1:8099/ \
     --server-host 100.108.169.118 \
     --output D:\mu-test\serve\manifest.json
   ```

5. Chạy web server: `python -m http.server 8099 --directory D:\mu-test\serve`
6. Chạy launcher GUI:

   ```sh
   src\ClientLauncher\bin\Debug\MUnique.OpenMU.ClientLauncher.exe --manifest http://127.0.0.1:8099/manifest.json
   ```

   Không truyền `--install-dir`/`--data-dir` thì launcher dùng đúng thư mục của người chơi
   (`%LOCALAPPDATA%\MuOnline`), giống bản phát hành thật. Muốn test lặp lại nhiều lần thì thêm
   `--install-dir D:\mu-test\client --data-dir D:\mu-test\state` để không đụng bản cài thật.
7. Bấm Start: launcher tải, verify SHA-256, giải nén, cài rồi chạy `Main.exe connect /u… /p…`.
8. Thử cập nhật: sửa một file trong `Data`, tạo archive data mới và sinh lại manifest với
   `--data-id` mới → mở lại launcher, nó phải tải lại archive data (~447 MB) rồi mới vào game.
9. Kiểm tra `%LOCALAPPDATA%\MuOnline\launcher.log`, `launcher-state.json` và thư mục `.cache`.

Bảng tuỳ chọn dòng lệnh của launcher:

| Tuỳ chọn | Ý nghĩa |
| --- | --- |
| `--manifest <url>` | Dùng manifest khác, ví dụ server test local |
| `--install-dir <path>` | Cài client vào thư mục khác |
| `--data-dir <path>` | Đổi chỗ chứa state/cache/log |
| `--update-only` (hoặc `--silent`) | Cập nhật rồi thoát, không mở giao diện (dùng cho automation) |

Các tuỳ chọn `--manifest`, `--install-dir` chỉ có hiệu lực cho lần chạy đó; chúng không được
ghi vào `launcher.config`.

## 14. Lộ trình

Trạng thái hiện tại: P1, P2 và P3 đã được triển khai trong `src/ClientLauncher/Updater/`,
kèm test ở `tests/MUnique.OpenMU.ClientLauncher.Tests/` và hai workflow
`.github/workflows/launcher-release.yml` (repo OpenMU) cùng
`client/.github/workflows/client-manifest.yml` + `client/tools/generate_manifest.py` (repo MuMain).
Hai workflow chưa chạy thật lần nào vì cần push lên GitHub.

| Giai đoạn | Nội dung | Ước lượng | Tiêu chí nghiệm thu |
| --- | --- | --- | --- |
| P1 | Manifest + cài lần đầu + cập nhật theo archive + log + UI tiến trình + start game | 4–6 ngày | Máy trắng cài và vào game được; lần mở sau dưới 2 giây; `config.ini` nguyên vẹn — **đã làm** |
| P2 | Resume, verify, "Cài lại sạch", rollback runtime, chẩn đoán | 2–3 ngày | Kịch bản 6, 7, 8, 10, 12 ở mục 13.3 đạt — **đã làm phần resume/verify/cài lại sạch; `--diagnostics` còn thiếu** |
| P3 | Self-update launcher + workflow phát hành launcher | 1–2 ngày | Kịch bản 9 đạt — **đã làm, chưa chạy thật lần nào** |
| P4 | Tuỳ chọn: ký số exe, bảng tin trong launcher, nguồn gương, dịch UI sang tiếng Việt | — | Giảm cảnh báo SmartScreen |

## 15. Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
| --- | --- | --- | --- |
| **Chỉ dùng archive từ GitHub (đang chọn)** | Không cần thêm hạ tầng, dễ hiểu, dễ chẩn đoán, update không cần Tailscale | Data đổi là tải lại 447 MB | **Chọn** |
| Delta từng file qua nginx trên Lenovo | Chỉ tải vài MB khi data đổi | Phải sinh manifest theo từng file, thêm nginx static, thêm cache hash, người chơi phải bật Tailscale khi cập nhật | Để dành |
| Chia data thành nhiều chunk archive trên GitHub | Không cần nginx, tải ít hơn 447 MB | Phải thêm workflow chia chunk, giới hạn 1.000 asset/release, phức tạp hơn | Để dành |
| Luồng patch FTP `C1 02` của OpenMU | Có sẵn trong server | Client MuMain không gọi; FTP không TLS, không delta | Loại |
| Launcher native C++ trong repo MuMain | Exe vài trăm KB, không cần .NET | Tốn thời gian phát triển hơn | Để sau |

Nếu sau này thấy 447 MB quá đau, nâng cấp lên delta từng file chỉ cần thêm `fileBaseUrl`
và `files[]` vào schema v2 rồi thêm một nhánh tải file lẻ trong launcher; phần cài đặt,
verify, apply và self-update giữ nguyên.

## 16. Câu hỏi cần bạn quyết

1. Thư mục cài mặc định: `%LOCALAPPDATA%\MuOnline\client`, hay để người chơi tự chọn?
2. Launcher có cần hiển thị bảng tin/thông báo từ server không, hay chỉ có nút Cập nhật và Start?
3. Ngôn ngữ UI launcher: tiếng Việt, tiếng Anh, hay cho chọn?
4. Có cần kênh `dev` riêng cho bạn tự test trước khi phát cho người chơi không?
5. Host/port mặc định nên lấy từ manifest (khuyến nghị) hay cố định trong launcher?
6. Launcher có giữ tuỳ chọn "ghi registry cho client cũ" như hiện tại không?
