# UNIT 31 — Architecture and Trade-offs

**Chủ đề tiếng Việt:** Kiến trúc và sự đánh đổi  
**Trình độ dự kiến:** B2 target | Integration  
**Thời lượng gợi ý:** 6 buổi × 60 phút; có thể học chậm hơn nếu cần  
**Đáp án riêng:** `ANSWER_KEYS/Unit_31_Answers.md`  
**Chế độ:** Text Teacher / Voice Teacher / Voice Partner / Mixed

## Mục tiêu / Learning outcomes

- Sử dụng **20 từ/cụm từ** thuộc chủ đề **kiến trúc và sự đánh đổi** và tự đặt câu có ngữ cảnh. 
- Phân biệt và dùng đúng **Expanded noun phrases** và **Participle clauses**.
- Đọc tình huống thực tế rồi trả lời câu hỏi bằng tiếng Anh.
- Viết đoạn/email theo yêu cầu, luyện hội thoại và nghe qua Voice khi có âm thanh thật.

## Kế hoạch 6 buổi / Six-session learning plan

| Buổi | Nội dung | Kết quả cần đạt |
|---|---|---|
| 1 | 10 từ đầu + Grammar 1 | Giải thích và viết 5 câu ví dụ |
| 2 | 10 từ tiếp + Grammar 2 | Phân biệt hai cấu trúc |
| 3 | Bài tập ngữ pháp + từ vựng | Tự làm, chưa xem đáp án |
| 4 | Reading + Writing | Trả lời 5 câu đọc, nộp bài viết |
| 5 | Voice Teacher + Partner | Nghe và nói theo tình huống Unit |
| 6 | Unit Test + chữa bài | Ghi lỗi sai và kế hoạch ôn vào PROGRESS.md |

## A. Vocabulary — Từ vựng (20 mục)

> IPA tham khảo giọng Anh-Anh, được tạo bằng công cụ phát âm; khi luyện Voice, hãy kiểm tra trọng âm và âm thực tế. Cột ngữ cảnh trích từ bài đọc nếu có; các mục khác yêu cầu tự đặt câu.

| STT | English / IPA | Nghĩa tiếng Việt | Ngữ cảnh / thực hành |
|---:|---|---|---|
| 1 | **architecture** /ˈɑːkɪtˌɛktʃə/ | kiến trúc | Đặt 1 câu của riêng bạn với từ này. |
| 2 | **scalability** /skˌeɪləbˈɪlɪti/ | khả năng mở rộng | Đặt 1 câu của riêng bạn với từ này. |
| 3 | **reliability** /ɹɪlˌaɪəbˈɪlɪti/ | độ tin cậy | Đặt 1 câu của riêng bạn với từ này. |
| 4 | **latency** /lˈeɪtənsi/ | độ trễ | Đặt 1 câu của riêng bạn với từ này. |
| 5 | **throughput** /θɹˈuːpʊt/ | thông lượng | Đặt 1 câu của riêng bạn với từ này. |
| 6 | **database** /dˈeɪtəbˌeɪs/ | cơ sở dữ liệu | They decided to add caching and improve database indexes first. |
| 7 | **cache** /kˈaʃ/ | bộ nhớ đệm | Đặt 1 câu của riêng bạn với từ này. |
| 8 | **queue** /kjˈuː/ | hàng đợi | Đặt 1 câu của riêng bạn với từ này. |
| 9 | **replication** /ɹˌɛplɪkˈeɪʃən/ | sao chép dữ liệu | Đặt 1 câu của riêng bạn với từ này. |
| 10 | **consistency** /kənsˈɪstənsi/ | tính nhất quán | Đặt 1 câu của riêng bạn với từ này. |
| 11 | **availability** /ɐvˌeɪləbˈɪlɪti/ | tính sẵn sàng | Đặt 1 câu của riêng bạn với từ này. |
| 12 | **partition** /pɑːtˈɪʃən/ | phân vùng | Đặt 1 câu của riêng bạn với từ này. |
| 13 | **service boundary** /sˈɜːvɪs bˈaʊndəɹi/ | ranh giới dịch vụ | Đặt 1 câu của riêng bạn với từ này. |
| 14 | **monolith** /mˈɒnəʊlˌɪθ/ | ứng dụng nguyên khối | Đặt 1 câu của riêng bạn với từ này. |
| 15 | **microservice** /mˌaɪkɹəʊsˈɜːvɪs/ | vi dịch vụ | Đặt 1 câu của riêng bạn với từ này. |
| 16 | **trade-off** /tɹˈeɪdˈɒf/ | đánh đổi | Đặt 1 câu của riêng bạn với từ này. |
| 17 | **failure mode** /fˈeɪliə mˈəʊd/ | kiểu hỏng | Đặt 1 câu của riêng bạn với từ này. |
| 18 | **maintenance** /mˈeɪntənəns/ | bảo trì | Đặt 1 câu của riêng bạn với từ này. |
| 19 | **cost efficiency** /kˈɒst ɪfˈɪʃənsi/ | hiệu quả chi phí | Đặt 1 câu của riêng bạn với từ này. |
| 20 | **observability** /ɒbzˌɜːvəbˈɪlɪti/ | khả năng quan sát hệ thống | Đặt 1 câu của riêng bạn với từ này. |

**Luyện từ / Vocabulary production:** Viết 8 câu: 3 câu đời sống, 3 câu công sở, 2 câu IT. Dùng từ của Unit và tự kiểm tra nghĩa, loại từ, cách phối hợp từ.

## B. Grammar — Giải thích song ngữ

### B1. Expanded noun phrases

- **Công thức / Form:** `determiner + modifiers + main noun + post-modifiers`
- **Cách dùng (tiếng Việt):** Gom thông tin kỹ thuật thành cụm danh từ mạch lạc nhưng tránh dồn quá nhiều bổ nghĩa.
- **Example:** *A scalable service with low operating costs is our goal.*
- **Nghĩa:** Mục tiêu là một dịch vụ có thể mở rộng với chi phí vận hành thấp.
- **Lỗi dễ mắc:** Tìm danh từ chính khi đọc cụm dài.

### B2. Participle clauses

- **Công thức / Form:** `V-ing / V3 phrase, main clause`
- **Cách dùng (tiếng Việt):** Rút gọn mệnh đề quan hệ hoặc diễn tả hoàn cảnh, khi chủ thể được hiểu rõ.
- **Example:** *Using caching, the team reduced database load.*
- **Nghĩa:** Bằng cách dùng cache, nhóm giảm tải cơ sở dữ liệu.
- **Lỗi dễ mắc:** Không để phân từ mở đầu bổ nghĩa nhầm chủ thể (dangling participle).

### B3. So sánh và tự kiểm tra

Trước khi chọn cấu trúc, tự hỏi: (1) thời gian nào, (2) sự thật hay giả định, (3) ai làm hành động, (4) điều gì cần nhấn mạnh?
Hãy giải thích lại bằng tiếng Việt hai câu mẫu phía trên, sau đó tự viết hai câu khác bằng tiếng Anh.

## C. Grammar practice — Bài tập ngữ pháp

**Quy tắc:** Chưa xem Answer Key. Trong chat, GPT giao **3–5 câu mỗi lượt** rồi chờ câu trả lời. **12 câu (8 điền từ + 4 sửa lỗi)**.

### C1. Fill in the blanks — Điền từ / chia động từ (8 câu)

1. ___ (use) caching, we reduced database reads.
2. The service ___ (design) for peak traffic is stable.
3. The team selected a solution with low ___ (operate) costs.
4. The data ___ (store) in the cache may be old.
5. ___ (compare) both options, we chose the simpler one.
6. A database ___ (handle) millions of records needs indexing.
7. The result ___ (measure) yesterday supports the change.
8. The engineers ___ (work) on the migration need support.

### C2. Error correction — Sửa lỗi (4 câu)

1. *Using caching, the latency was reduced by our team.*
2. *The system designing for mobile is slow.*
3. *The engineer worked on the feature need help.*
4. *A service scalable and reliable we need.*

## D. Vocabulary practice — Ôn từ vựng

**D1. Viết từ/cụm tiếng Anh phù hợp với nghĩa tiếng Việt (5 câu).** Không nhìn bảng A trước khi làm.

1. kiến trúc → ______
2. khả năng mở rộng → ______
3. độ tin cậy → ______
4. độ trễ → ______
5. thông lượng → ______

**D2. Apply the words — Ứng dụng:** Chọn 3 từ bất kỳ trong bảng A, viết một đoạn hội thoại 5 lượt có chứa 3 từ đó. Viết thêm 2 câu với từ mới chưa dùng.

## E. Reading — Đọc hiểu

**Cách học:** Đọc lượt một lấy ý chính; đọc lượt hai tìm chứng cứ. Không dịch từng từ theo thói quen.

> A growing e-commerce platform faced slow response times during weekend promotions. Engineers considered two approaches. The first was to optimize the existing monolithic application and add caching for frequently requested product data. The second was to split the catalog into a separate service. A new service might scale independently, but it would increase operational complexity and require reliable communication between services. The team examined traffic patterns and discovered that most slow requests were reading the same data repeatedly. They decided to add caching and improve database indexes first. They agreed to revisit service separation if traffic continued to grow. The decision prioritized a measured improvement over unnecessary architectural complexity.

### Reading questions — Trả lời bằng tiếng Anh (5 câu)

1. When did the platform become slow?
2. What two approaches were considered?
3. What risk came with a separate service?
4. What pattern did the team discover?
5. What did the team decide to do first?

**Reading extension:** Tóm tắt đoạn đọc bằng 3 câu tiếng Anh. Dùng ít nhất hai từ mới của Unit.

## F. Writing — Bài viết có hướng dẫn

**Writing assignment:** Write a short architecture decision record with context, options, trade-offs, decision and review trigger.

**Trước khi viết:** lập dàn ý bằng 3–5 gạch đầu dòng; kiểm tra thì/cấu trúc, từ nối và sự rõ ràng.

**Rubric (100 điểm):** Nội dung & nhiệm vụ 40; ngữ pháp trọng tâm 35; từ vựng, mạch lạc và giọng điệu 25. GPT chỉ cung cấp bản sửa sau khi bạn đã nộp.

## G. Speaking & Listening — Voice và văn bản

### G1. Warm-up & pronunciation

- Chọn 5 mục từ trong bảng A: nghe GPT đọc từng mục và lặp lại nếu Voice có phát âm thanh.
- Với mỗi từ, đọc một câu chứa từ đó; giáo viên giải thích trọng âm/cụm nối âm khi cần.
- **Không tự chấm phát âm** nếu GPT chỉ nhận văn bản hoặc transcript.

### G2. Partner role-play / Hội thoại tình huống

- **Role for GPT (English):** You are a technical reviewer. Compare a monolith and microservices, ask about failure modes, and challenge my assumptions.
- **Hướng dẫn tiếng Việt:** GPT đóng vai đối thoại, hỏi mỗi lần 1 câu và chờ học viên trả lời; chỉ sửa lỗi cản trở hiểu trong lúc nói, còn lại tổng kết ở cuối.
- **Text Mode:** Thực hành dưới dạng chat, chấm độ rõ và ngữ pháp; **không tính** phát âm hoặc nghe.
- **Voice Mode:** Giữ 80–90% đối thoại bằng tiếng Anh; giải thích tiếng Việt theo yêu cầu.

### G3. Listening check — Nghe hiểu qua Voice

- Đề nghị GPT nói **một thông điệp mới 60–90 từ** theo chủ đề này, không hiện bản chép trước khi nghe; sau đó trả lời ba câu về **vấn đề / hành động / bước tiếp theo**.
- GPT có thể dựa vào tình huống gợi ý trong Answer Key để tạo nội dung mới, **không đọc nguyên bài Reading** đã thấy.
- Chỉ ghi `Listening: đã kiểm tra` khi người học **thực sự nghe âm thanh** trước khi thấy chữ.

**Lệnh Voice:** `Voice Partner Mode, Unit 31. You are a technical reviewer. Compare a monolith and microservices, ask about failure modes, and challenge my assumptions. Ask one question at a time; give feedback in Vietnamese after the role-play.`

## H. Unit Test — Điều kiện chuyển Unit

- Grammar C1 + C2: **__/12** (mục tiêu ≥10/12 và hiểu cách sửa lỗi).
- Vocabulary D1: **__/5** (mục tiêu ≥4/5).
- Reading E: **__/5** (mục tiêu ≥4/5).
- Writing: **Đạt / Cần ôn / Chưa làm** theo rubric.
- Speaking: **Đạt / Cần ôn / Chưa làm**. Listening: **Đạt / Cần ôn / Chưa đánh giá**.
- Sau Unit 12, 18, 24, 30, 36 thực hiện thêm đánh giá tổng hợp phần tương ứng.
- Nếu chưa đạt, học tiếp buổi ôn và tạo câu kiểm tra mới, không chỉ học thuộc đáp án.

## I. Homework & spaced review — Bài về nhà

- Ôn từ sau 1, 3, 7, 14 ngày dựa trên ngày học thực tế; viết 5 câu mới không nhìn bảng.
- Ghi 3 lỗi tiêu biểu vào `TRACKING/PROGRESS.md`; kèm câu sửa và lý do tiếng Việt.
- Trước Unit tiếp theo, nói lại nội dung Unit hiện tại bằng 60–90 giây tiếng Anh.

