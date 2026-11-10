# UNIT 19 — Software Development Lifecycle

**Chủ đề tiếng Việt:** Vòng đời phát triển phần mềm  
**Trình độ dự kiến:** B1+ | IT  
**Thời lượng gợi ý:** 6 buổi × 60 phút; có thể học chậm hơn nếu cần  
**Đáp án riêng:** `ANSWER_KEYS/Unit_19_Answers.md`  
**Chế độ:** Text Teacher / Voice Teacher / Voice Partner / Mixed

## Mục tiêu / Learning outcomes

- Sử dụng **20 từ/cụm từ** thuộc chủ đề **vòng đời phát triển phần mềm** và tự đặt câu có ngữ cảnh. 
- Phân biệt và dùng đúng **Tense contrast in development** và **Present perfect for delivery**.
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
| 1 | **backlog** /bˈaklɒɡ/ | danh sách công việc | Đặt 1 câu của riêng bạn với từ này. |
| 2 | **user story** /jˈuːzə stˈɔːɹi/ | câu chuyện người dùng | In the product team, every feature begins with a user story. |
| 3 | **ticket** /tˈɪkɪt/ | phiếu công việc | Đặt 1 câu của riêng bạn với từ này. |
| 4 | **repository** /ɹɪpˈɒsɪtəɹi/ | kho mã nguồn | Đặt 1 câu của riêng bạn với từ này. |
| 5 | **branch** /bɹˈantʃ/ | nhánh mã | A developer then creates a branch and starts implementing the change. |
| 6 | **commit** /kəmˈɪt/ | lần ghi nhận mã | Đặt 1 câu của riêng bạn với từ này. |
| 7 | **pull request** /pˈʊl ɹɪkwˈɛst/ | yêu cầu gộp mã | When the code is ready, the developer opens a pull request for review. |
| 8 | **code review** /kˈəʊd ɹɪvjˈuː/ | kiểm tra mã | Đặt 1 câu của riêng bạn với từ này. |
| 9 | **merge** /mˈɜːdʒ/ | gộp nhánh | Đặt 1 câu của riêng bạn với từ này. |
| 10 | **build** /bˈɪld/ | bản dựng | After the review is approved, the change is merged and the pipeline builds a new version for staging. |
| 11 | **pipeline** /pˈaɪplaɪn/ | chuỗi tự động hóa | After the review is approved, the change is merged and the pipeline builds a new version for staging. |
| 12 | **staging** /stˈeɪdʒɪŋ/ | môi trường thử nghiệm | After the review is approved, the change is merged and the pipeline builds a new version for staging. |
| 13 | **release** /ɹɪlˈiːs/ | bản phát hành | QA tests it before the release. |
| 14 | **iteration** /ˌɪtəɹˈeɪʃən/ | vòng lặp | Đặt 1 câu của riêng bạn với từ này. |
| 15 | **sprint** /spɹˈɪnt/ | chu kỳ phát triển | Đặt 1 câu của riêng bạn với từ này. |
| 16 | **acceptance criteria** /ɐksˈɛptəns kɹaɪtˈiəɹɪə/ | tiêu chí nghiệm thu | Đặt 1 câu của riêng bạn với từ này. |
| 17 | **refactor** /ɹɪfˈaktə/ | tái cấu trúc mã | Đặt 1 câu của riêng bạn với từ này. |
| 18 | **technical debt** /tˈɛknɪkəl dˈɛt/ | nợ kỹ thuật | Đặt 1 câu của riêng bạn với từ này. |
| 19 | **version control** /vˈɜːʃən kəntɹˈəʊl/ | quản lý phiên bản | Đặt 1 câu của riêng bạn với từ này. |
| 20 | **workflow** /wˈɜːkfləʊ/ | quy trình làm việc | Đặt 1 câu của riêng bạn với từ này. |

**Luyện từ / Vocabulary production:** Viết 8 câu: 3 câu đời sống, 3 câu công sở, 2 câu IT. Dùng từ của Unit và tự kiểm tra nghĩa, loại từ, cách phối hợp từ.

## B. Grammar — Giải thích song ngữ

### B1. Tense contrast in development

- **Công thức / Form:** `present simple (routine); present continuous (current); past simple (finished event)`
- **Cách dùng (tiếng Việt):** Đổi thì theo từng giai đoạn của quy trình: bước lặp lại, việc đang làm, việc đã hoàn thành.
- **Example:** *We review every PR. We are reviewing this one now. We merged another yesterday.*
- **Nghĩa:** Chúng tôi review mọi PR. Hiện review PR này. Hôm qua đã gộp PR khác.
- **Lỗi dễ mắc:** Từ chỉ thời gian quyết định thì phù hợp.

### B2. Present perfect for delivery

- **Công thức / Form:** `have/has + V3 + already/yet/just`
- **Cách dùng (tiếng Việt):** Dùng để báo trạng thái kết quả đến hiện tại, đặc biệt khi trả lời câu hỏi “đã xong chưa?”.
- **Example:** *We have just deployed the fix to staging.*
- **Nghĩa:** Chúng tôi vừa triển khai bản sửa lên staging.
- **Lỗi dễ mắc:** Không dùng present perfect với yesterday hoặc last week.

### B3. So sánh và tự kiểm tra

Trước khi chọn cấu trúc, tự hỏi: (1) thời gian nào, (2) sự thật hay giả định, (3) ai làm hành động, (4) điều gì cần nhấn mạnh?
Hãy giải thích lại bằng tiếng Việt hai câu mẫu phía trên, sau đó tự viết hai câu khác bằng tiếng Anh.

## C. Grammar practice — Bài tập ngữ pháp

**Quy tắc:** Chưa xem Answer Key. Trong chat, GPT giao **3–5 câu mỗi lượt** rồi chờ câu trả lời. **12 câu (8 điền từ + 4 sửa lỗi)**.

### C1. Fill in the blanks — Điền từ / chia động từ (8 câu)

1. We usually ___ (merge) code after approval.
2. She ___ (review) a PR right now.
3. The build ___ (fail) yesterday afternoon.
4. They ___ (already / deploy) the feature to staging.
5. He ___ (not / finish) the ticket yet.
6. Our pipeline ___ (run) tests every night.
7. I ___ (open) a pull request two hours ago.
8. We ___ (refactor) the payment module this week.

### C2. Error correction — Sửa lỗi (4 câu)

1. *We are usually deploy on Tuesday.*
2. *He has merged it yesterday.*
3. *The pipeline run every night.*
4. *I am review the code now.*

## D. Vocabulary practice — Ôn từ vựng

**D1. Viết từ/cụm tiếng Anh phù hợp với nghĩa tiếng Việt (5 câu).** Không nhìn bảng A trước khi làm.

1. danh sách công việc → ______
2. câu chuyện người dùng → ______
3. phiếu công việc → ______
4. kho mã nguồn → ______
5. nhánh mã → ______

**D2. Apply the words — Ứng dụng:** Chọn 3 từ bất kỳ trong bảng A, viết một đoạn hội thoại 5 lượt có chứa 3 từ đó. Viết thêm 2 câu với từ mới chưa dùng.

## E. Reading — Đọc hiểu

**Cách học:** Đọc lượt một lấy ý chính; đọc lượt hai tìm chứng cứ. Không dịch từng từ theo thói quen.

> In the product team, every feature begins with a user story. During planning, the product owner explains the expected behavior and the team estimates the effort. A developer then creates a branch and starts implementing the change. When the code is ready, the developer opens a pull request for review. Another engineer checks readability, tests and possible edge cases. After the review is approved, the change is merged and the pipeline builds a new version for staging. QA tests it before the release. Last month, the team noticed that reviews were taking too long. They are now experimenting with smaller pull requests, and early results suggest that feedback is faster.

### Reading questions — Trả lời bằng tiếng Anh (5 câu)

1. What starts the development process?
2. Who explains expected behavior?
3. What happens before merging code?
4. Where is a new version tested before release?
5. What improvement is the team trying now?

**Reading extension:** Tóm tắt đoạn đọc bằng 3 câu tiếng Anh. Dùng ít nhất hai từ mới của Unit.

## F. Writing — Bài viết có hướng dẫn

**Writing assignment:** Describe the lifecycle of one feature using at least three different tenses and five development terms.

**Trước khi viết:** lập dàn ý bằng 3–5 gạch đầu dòng; kiểm tra thì/cấu trúc, từ nối và sự rõ ràng.

**Rubric (100 điểm):** Nội dung & nhiệm vụ 40; ngữ pháp trọng tâm 35; từ vựng, mạch lạc và giọng điệu 25. GPT chỉ cung cấp bản sửa sau khi bạn đã nộp.

## G. Speaking & Listening — Voice và văn bản

### G1. Warm-up & pronunciation

- Chọn 5 mục từ trong bảng A: nghe GPT đọc từng mục và lặp lại nếu Voice có phát âm thanh.
- Với mỗi từ, đọc một câu chứa từ đó; giáo viên giải thích trọng âm/cụm nối âm khi cần.
- **Không tự chấm phát âm** nếu GPT chỉ nhận văn bản hoặc transcript.

### G2. Partner role-play / Hội thoại tình huống

- **Role for GPT (English):** You are a new developer asking about how a feature travels from ticket to production. I explain each step.
- **Hướng dẫn tiếng Việt:** GPT đóng vai đối thoại, hỏi mỗi lần 1 câu và chờ học viên trả lời; chỉ sửa lỗi cản trở hiểu trong lúc nói, còn lại tổng kết ở cuối.
- **Text Mode:** Thực hành dưới dạng chat, chấm độ rõ và ngữ pháp; **không tính** phát âm hoặc nghe.
- **Voice Mode:** Giữ 80–90% đối thoại bằng tiếng Anh; giải thích tiếng Việt theo yêu cầu.

### G3. Listening check — Nghe hiểu qua Voice

- Đề nghị GPT nói **một thông điệp mới 60–90 từ** theo chủ đề này, không hiện bản chép trước khi nghe; sau đó trả lời ba câu về **vấn đề / hành động / bước tiếp theo**.
- GPT có thể dựa vào tình huống gợi ý trong Answer Key để tạo nội dung mới, **không đọc nguyên bài Reading** đã thấy.
- Chỉ ghi `Listening: đã kiểm tra` khi người học **thực sự nghe âm thanh** trước khi thấy chữ.

**Lệnh Voice:** `Voice Partner Mode, Unit 19. You are a new developer asking about how a feature travels from ticket to production. I explain each step. Ask one question at a time; give feedback in Vietnamese after the role-play.`

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

