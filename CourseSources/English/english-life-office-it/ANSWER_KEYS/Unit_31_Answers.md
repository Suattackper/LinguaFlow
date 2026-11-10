# ANSWER KEY — UNIT 31: Architecture and Trade-offs

**Lưu ý:** Tài liệu dành cho giáo viên/GPT và học viên sau khi đã tự làm. Các đáp án có dấu `/` là phương án được chấp nhận tùy ngữ cảnh.

## C1. Grammar fill in the blanks (8)

| Câu | Đáp án | Giải thích tiếng Việt |
|---:|---|---|
| 1 | **Using** | V-ing phrase bổ nghĩa we. |
| 2 | **designed** | Rút gọn bị động: service designed... |
| 3 | **operating** | Operating costs là cụm danh từ chuẩn. |
| 4 | **stored** | Dữ liệu được lưu, dùng V3. |
| 5 | **Comparing / After comparing** | Phân từ có cùng chủ thể we. |
| 6 | **handling** | Rút gọn chủ động bằng V-ing. |
| 7 | **measured** | Rút gọn bị động bằng V3. |
| 8 | **working** | Rút gọn mệnh đề xác định người. |

## C2. Error correction (4)

| Câu | Câu sửa đúng | Lý do bằng tiếng Việt |
|---:|---|---|
| 1 | **Using caching, our team reduced latency.** | Chủ thể của using phải là team. |
| 2 | **The system designed for mobile is slow.** | Hệ thống được thiết kế, dùng V3. |
| 3 | **The engineer working on the feature needs help.** | Rút gọn đúng và chia needs. |
| 4 | **We need a scalable and reliable service.** | Trật tự cụm danh từ và câu tự nhiên. |

## D1. Vocabulary practice (5)

| Câu | English | Vietnamese |
|---:|---|---|
| 1 | **architecture** | kiến trúc |
| 2 | **scalability** | khả năng mở rộng |
| 3 | **reliability** | độ tin cậy |
| 4 | **latency** | độ trễ |
| 5 | **throughput** | thông lượng |

## E. Reading — Suggested answers (5)

1. **During weekend promotions.**

2. **Optimize the monolith with caching or separate the catalog service.**

3. **Higher operational complexity.**

4. **Repeated reads of the same data.**

5. **Add caching and improve database indexes.**

## F. Writing — Hướng dẫn chấm bài mở

- **Yêu cầu đề:** Write a short architecture decision record with context, options, trade-offs, decision and review trigger.
- **Nội dung (40%):** Đủ yêu cầu, có chi tiết kiểm chứng hoặc ví dụ; không lan man.
- **Ngữ pháp (35%):** Dùng đúng cấu trúc trong B1/B2; chấp nhận cách diễn đạt khác vẫn đúng.
- **Từ vựng & mạch lạc (25%):** Từ đúng nghĩa, register phù hợp, nối ý rõ.
- **Câu mẫu 1:** *A scalable service with low operating costs is our goal.*
- **Câu mẫu 2:** *Using caching, the team reduced database load.*
- **Cách chữa:** Trích câu gốc → sửa tối thiểu → giải thích tiếng Việt → yêu cầu học viên tự diễn đạt lại.
- Đây là bài viết mở, không có một đoạn văn duy nhất đúng cho mọi học viên.

## G. Speaking / Listening — Assessor notes

- **Bối cảnh:** You are a technical reviewer. Compare a monolith and microservices, ask about failure modes, and challenge my assumptions.
- **Thang speaking (100 điểm):** hoàn thành nhiệm vụ/duy trì tương tác 40; từ vựng phù hợp 30; ngữ pháp 30.
- **Giọng nói/phát âm:** chỉ nhận xét khi có âm thanh đủ tin cậy. Nếu transcript-only ghi chưa đánh giá.
- **Nghe:** chỉ đánh giá nếu kịch bản đã được phát thành âm thanh trước khi hiển thị chữ.
- **Gợi ý listening task riêng:** GPT nói một voice note ngắn về chủ đề Unit với ba ý mới: vấn đề, cách xử lý, bước tiếp theo. Hỏi học viên ba câu tương ứng.
- **Chấm listening:** 1 điểm cho mỗi ý chính đúng và 1 điểm nếu có chi tiết thời gian/nguyên nhân; câu trả lời tương đương đều được chấp nhận.

## H. Scoring & remediation

- Grammar: **12 câu**, Vocabulary: **5 câu**, Reading: **5 câu**. Nếu grammar dưới 10/12 hoặc vocab/reading dưới 4/5, dạy lại đúng lỗi và kiểm tra câu mới.
- Câu trả lời không nằm đúng chữ trong answer key nhưng đúng ngữ nghĩa/ngữ pháp vẫn phải được chấp nhận.
- Sau khi chấm, giáo viên nêu **3 lỗi ưu tiên**, **3 cụm cần ôn**, **1 mục tiêu cho buổi tới**.

