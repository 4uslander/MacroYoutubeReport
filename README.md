# MacroYoutubeReport

Hệ thống tự động hóa báo cáo vi phạm (Report) kênh và video YouTube theo các kịch bản định trước, hỗ trợ đa nền tảng (Android thật/giả lập qua ADB và Web Chrome qua Playwright) với cơ chế tương tác mô phỏng người thật 100%.

## 1. Kiến Trúc Dự Án (Clean Architecture)

- **`MacroYoutubeReport.Core`**:
  - Định nghĩa giao diện điều khiển `IYouTubeReportDriver`.
  - Bộ mô hình dữ liệu: `ReportTargetItem`, `ReportReasonConfig`, `ReportScenarioConfig`, `NaturalEngagementOptions`, `ReportStepResult`, `ReportTaskRecord`.
  - Hệ thống Enums: `PlatformType`, `ReportReasonCategory`, `ReportTargetType`, `DeviceStatus`, `ExecutionStatus`, `AppLanguage`.
  - Động cơ kịch bản: `WorkflowEngine`, `IWorkflowStep`, `WorkflowContext`, `ScenarioDefinitions` (Hỗ trợ 3 luồng kịch bản: Tìm kiếm & xem tự nhiên trước khi report, Mở link trực tiếp, Báo cáo kênh người dùng).
  - Quản lý từ điển giao diện đa ngôn ngữ: `UiLanguageManager` (hỗ trợ Tiếng Việt, Tiếng Anh, Tiếng Hàn).
- **`MacroYoutubeReport.Driver.Android`**:
  - Điều khiển thiết bị Android qua `AdvancedSharpAdbClient`.
  - Quản lý XML UI dump, Locators và các bước tương tác với app YouTube.
- **`MacroYoutubeReport.Driver.Chrome`**:
  - Điều khiển trình duyệt Chrome thông qua Playwright headless / headed profile.
- **`MacroYoutubeReport.Scheduler`**:
  - Bộ điều phối hàng đợi tác vụ `ReportTaskCoordinator`.
  - Lưu trữ lịch sử nhật ký bền vững `SqliteExecutionJournal`.
- **`MacroYoutubeReport.UI.WinForms`**:
  - Giao diện người dùng WinForms trực quan (MVP Pattern).
- **`MacroYoutubeReport.Tests`**:
  - Bộ kiểm thử đơn vị xUnit xác thực Models, Locators, Scenarios và SQLite Journal.

## 2. Quy Trình Thao Tác Mô Phỏng Người Thật (Human-Like Report)

1. **Khởi tạo & Chuẩn bị**: Mở ứng dụng YouTube, tự động vượt qua popup cản trở và thoát bẫy Shorts.
2. **Tìm kiếm tự nhiên**: Gõ từ khóa vào thanh search với tốc độ gõ phím ngẫu nhiên.
3. **Cuộn thông minh**: Cuộn danh sách kết quả, nhận diện chính xác video theo Tiêu đề hoặc Kênh, click mở với độ lệch ngẫu nhiên.
4. **Xem tự nhiên (Warm-up Watch Time)**: Cho video phát trong khoảng thời gian định trước (30s - 180s) kết hợp cuộn đọc comment hoặc tạm dừng nhẹ để tạo tín hiệu người thật.
5. **Kích hoạt Báo cáo**: Chạm hiện menu player -> mở mục "Báo cáo" (Report / 신고).
6. **Chọn lý do & Giải trình**: Chọn danh mục vi phạm (Spam, Hateful, Harassment, Misinformation, v.v.), nhập ghi chú giải trình (hỗ trợ Spin-text tự động).
7. **Xác nhận & Bắt bằng chứng**: Bấm nút gửi báo cáo, chụp ảnh màn hình lưu bằng chứng (Proof Screenshot) và ghi nhật ký vào SQLite.

## 3. Lệnh Build & Chạy Test

```bash
# Build toàn bộ Solution
dotnet build MacroYoutubeReport.sln

# Chạy kiểm thử tự động
dotnet test MacroYoutubeReport.sln
```
