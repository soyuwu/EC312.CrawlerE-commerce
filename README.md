# Mallow Category Crawler

Console application .NET dùng để lấy toàn bộ sản phẩm trong category của Mallow, crawl các biến thể (màu, size) rồi xuất file CSV có thể import vào WooCommerce.

## Yêu cầu

- .NET SDK 10.0
- PowerShell (chỉ cần khi cài trình duyệt Playwright)

## Cài đặt

Khôi phục package và build dự án:

```powershell
dotnet restore
dotnet build
```

Cài browser Chromium cho Playwright (chạy một lần sau khi build):

```powershell
pwsh .\bin\Debug\net10.0\playwright.ps1 install chromium
```

## Cách chạy

### Crawl một category

```powershell
dotnet run -- --category "https://mallow.vn/<category-url>"
```

Ví dụ:

```powershell
dotnet run -- --category "https://mallow.vn/quan-nam.html"
```

Chương trình sẽ lần lượt:

1. Lấy tất cả sản phẩm qua các trang phân trang của category.
2. Mở từng trang sản phẩm để lấy thông tin và biến thể.
3. Gọi API của Mallow để lấy thông tin biến thể.
4. Xuất kết quả vào `mallow-products.csv` và dữ liệu sản phẩm trung gian vào `mallow-products.json`.

Tên category trên các dòng CSV được suy ra từ URL category.

### Crawl một sản phẩm để kiểm tra

```powershell
dotnet run -- --product "https://mallow.vn/<product-slug>-p123456.html"
```

### Crawl nhiều category

Tạo file văn bản, mỗi dòng theo định dạng `Tên category|URL category`:

```text
Quần nam|https://mallow.vn/quan-nam.html
Áo nam|https://mallow.vn/ao-nam.html
```

Sau đó chạy:

```powershell
dotnet run -- --categories .\categories.txt
```

Nếu không chỉ định đường dẫn file, chương trình mặc định đọc `Data/product-urls.txt`.

## Kết quả xuất

`mallow-products.csv` gồm:

- Một dòng sản phẩm cha cho mỗi sản phẩm Mallow.
- Một dòng variation cho mỗi biến thể tìm được.
- SKU, tên, giá, tồn kho, ảnh, màu, size và category.

Sản phẩm hoặc biến thể không crawl được sẽ được ghi lỗi ra console; các mục còn lại vẫn tiếp tục được xử lý.

## Lưu ý

- Không commit `bin/`, `obj/`, `mallow-products.csv` hay `mallow-products.json`; chúng đã được khai báo trong `.gitignore`.
- Crawl toàn bộ category có thể mất thời gian vì ứng dụng có delay giữa các request để hạn chế tải lên website nguồn.
