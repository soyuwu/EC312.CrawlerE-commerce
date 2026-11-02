# Mallow WooCommerce Exporter

Ứng dụng Windows Forms lấy dữ liệu sản phẩm từ Mallow và xuất tệp CSV theo định dạng có thể nhập vào WooCommerce. Tool hỗ trợ lấy một sản phẩm, một category hoặc nhiều category cùng lúc; đồng thời tạo tệp JSON trung gian để đối chiếu dữ liệu.

## Yêu cầu

- Windows 10/11.
- .NET SDK 10.0 nếu chạy từ mã nguồn.
- Chromium cho Playwright nếu tool chưa được cài browser. Chromium chạy nền, không mở cửa sổ trình duyệt.

## Chạy từ mã nguồn

1. Mở PowerShell tại thư mục dự án và cài các package:

   ```powershell
   dotnet restore
   ```

2. Build solution:

   ```powershell
   dotnet build .\Lab3.sln
   ```

3. Cài Chromium cho Playwright (chỉ cần làm một lần trên mỗi máy):

   ```powershell
   pwsh .\Lab3.WinForms\bin\Debug\net10.0-windows\playwright.ps1 install chromium
   ```

4. Chạy giao diện:

   ```powershell
   dotnet run --project .\Lab3.WinForms\Lab3.WinForms.csproj
   ```

Bạn cũng có thể mở `Lab3.sln` bằng Visual Studio, đặt `Lab3.WinForms` làm Startup Project và nhấn **F5**.

## Cách sử dụng

1. Mở ứng dụng **Mallow WooCommerce Exporter**.
2. Chọn một trong ba mục tại ô **Chế độ**.
3. Nhập URL hoặc chọn file danh sách tương ứng.
4. Chọn thư mục tại ô **Xuất ra**. Mặc định là thư mục `output` cạnh file chạy tool.
5. Nhấn **Bắt đầu** và chờ thông báo hoàn tất. Có thể nhấn **Dừng** để hủy tác vụ đang chạy.
6. Nhấn **Mở thư mục kết quả** để xem file xuất.

### 1. Một sản phẩm

Dùng khi chỉ cần xuất một trang sản phẩm. Dán URL sản phẩm Mallow, URL phải có mã sản phẩm ở cuối, ví dụ:

```text
https://mallow.vn/ten-san-pham-p39128355.html
```

### 2. Một category

Dùng để lấy toàn bộ sản phẩm trong một category, bao gồm các trang phân trang. Dán URL category Mallow, ví dụ:

```text
https://mallow.vn/quan-nam.html
```

### 3. File nhiều category

Chọn một file `.txt`. Mỗi dòng theo định dạng:

```text
Tên category|URL category
```

Ví dụ:

```text
Quần nam|https://mallow.vn/quan-nam.html
Áo nam|https://mallow.vn/ao-nam.html
```

Dòng trống và dòng bắt đầu bằng `#` sẽ được bỏ qua. Một sản phẩm xuất hiện ở nhiều category chỉ được xử lý một lần.

## Kết quả xuất

Trong thư mục đã chọn, tool sẽ ghi đè hai file sau ở mỗi lần chạy:

- `mallow-products.csv`: dữ liệu sản phẩm và biến thể, gồm các cột theo chuẩn import của WooCommerce.
- `mallow-products.json`: dữ liệu trung gian để kiểm tra hoặc đối chiếu.

Khi import vào WooCommerce, chọn `mallow-products.csv` trong mục **Products → Import**. Nên kiểm tra trước bằng một category nhỏ để xác nhận mapping phù hợp với website WooCommerce đích.

## Chạy bản đã publish

Nếu nhận tool ở dạng thư mục publish, chạy file `Lab3.WinForms.exe`. Lần đầu trên một máy, mở PowerShell ngay trong thư mục chứa file `.exe` và cài Chromium:

```powershell
pwsh .\playwright.ps1 install chromium
```

Nếu Windows chặn script, chạy lệnh sau chỉ cho cửa sổ PowerShell hiện tại, rồi chạy lại lệnh cài browser:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

## Lưu ý

- Cần kết nối Internet trong lúc crawl.
- Quét category lớn có thể mất nhiều thời gian; không tắt ứng dụng trước khi hoàn tất.
- Một số sản phẩm hoặc biến thể không đọc được sẽ được ghi trong khung nhật ký; các mục còn lại vẫn tiếp tục xử lý.
- Không commit thư mục `bin/`, `obj/`, `output/` hay các file dữ liệu xuất vào Git.
