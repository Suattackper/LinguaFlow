# UNIT 20 — Bug Investigation

**Chủ đề tiếng Việt:** Điều tra lỗi phần mềm  
**Trình độ dự kiến:** B1+ | IT  
**Thời lượng gợi ý:** 6 buổi × 60 phút; có thể học chậm hơn nếu cần  
**Đáp án riêng:** `ANSWER_KEYS/Unit_20_Answers.md`  
**Chế độ:** Text Teacher / Voice Teacher / Voice Partner / Mixed

## Mục tiêu / Learning outcomes

- Sử dụng **20 từ/cụm từ** thuộc chủ đề **điều tra lỗi phần mềm** và tự đặt câu có ngữ cảnh. 
- Phân biệt và dùng đúng **Cause and effect** và **Purpose and result**.
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
| 1 | **reproduce** /ɹɪpɹədjˈuːs/ | tái hiện | The bug appeared only on older Android devices, so the developer could not reproduce it on his own phone. |
| 2 | **stack trace** /stˈak tɹˈeɪs/ | dấu vết ngăn xếp | Đặt 1 câu của riêng bạn với từ này. |
| 3 | **log** /lˈɒɡ/ | nhật ký | The logs showed a memory error after a very large photo was selected. |
| 4 | **exception** /ɛksˈɛpʃən/ | ngoại lệ | Đặt 1 câu của riêng bạn với từ này. |
| 5 | **severity** /səvˈɛɹɪti/ | mức độ nghiêm trọng | Đặt 1 câu của riêng bạn với từ này. |
| 6 | **priority** /pɹaɪˈɒɹɪti/ | độ ưu tiên | Đặt 1 câu của riêng bạn với từ này. |
| 7 | **regression** /ɹɪɡɹˈɛʃən/ | lỗi tái xuất hiện | Đặt 1 câu của riêng bạn với từ này. |
| 8 | **root cause** /ɹˈuːt kˈɔːz/ | nguyên nhân gốc | Đặt 1 câu của riêng bạn với từ này. |
| 9 | **workaround** /wˈɜːkɐɹˌaʊnd/ | giải pháp tạm | They also provided a temporary workaround: customers could upload smaller photos until the update was released. |
| 10 | **environment** /ɛnvˈaɪɹənmənt/ | môi trường | Đặt 1 câu của riêng bạn với từ này. |
| 11 | **configuration** /kənfˌɪɡjəɹˈeɪʃən/ | cấu hình | Đặt 1 câu của riêng bạn với từ này. |
| 12 | **screenshot** /skɹˈiːnʃɒt/ | ảnh chụp màn hình | Đặt 1 câu của riêng bạn với từ này. |
| 13 | **timestamp** /tˈaɪmstamp/ | mốc thời gian | Đặt 1 câu của riêng bạn với từ này. |
| 14 | **expected behavior** /ɛkspˈɛktɪd bɪhˈeɪvjə/ | hành vi mong đợi | Đặt 1 câu của riêng bạn với từ này. |
| 15 | **actual behavior** /ˈaktʃuːəl bɪhˈeɪvjə/ | hành vi thực tế | Đặt 1 câu của riêng bạn với từ này. |
| 16 | **crash** /kɹˈaʃ/ | sập chương trình | A customer reported that the mobile app sometimes crashed when uploading a photo. |
| 17 | **intermittent** /ˌɪntəmˈɪtənt/ | không liên tục | Đặt 1 câu của riêng bạn với từ này. |
| 18 | **diagnose** /dˌaɪəɡnˈəʊz/ | chẩn đoán | Đặt 1 câu của riêng bạn với từ này. |
| 19 | **trace** /tɹˈeɪs/ | truy vết | Đặt 1 câu của riêng bạn với từ này. |
| 20 | **fix** /fˈɪks/ | bản sửa | The team created a fix that resized images earlier and added a test using large files. |

**Luyện từ / Vocabulary production:** Viết 8 câu: 3 câu đời sống, 3 câu công sở, 2 câu IT. Dùng từ của Unit và tự kiểm tra nghĩa, loại từ, cách phối hợp từ.

## B. Grammar — Giải thích song ngữ

### B1. Cause and effect

- **Công thức / Form:** `because / since + clause; because of / due to + noun phrase`
- **Cách dùng (tiếng Việt):** Nói nguyên nhân bằng because + mệnh đề; because of/due to + danh từ hoặc cụm danh từ.
- **Example:** *The app crashed because memory usage increased.*
- **Nghĩa:** Ứng dụng bị sập vì mức sử dụng bộ nhớ tăng.
- **Lỗi dễ mắc:** Không viết because of the app crashed.

### B2. Purpose and result

- **Công thức / Form:** `so that + clause; therefore/as a result + sentence`
- **Cách dùng (tiếng Việt):** Phân biệt mục đích và kết quả: so that nói vì mục đích gì, therefore nói kết quả logic.
- **Example:** *We collected logs so that we could reproduce the bug.*
- **Nghĩa:** Chúng tôi thu thập log để có thể tái hiện lỗi.
- **Lỗi dễ mắc:** Therefore thường nối hai câu độc lập với dấu câu phù hợp.

### B3. So sánh và tự kiểm tra

Trước khi chọn cấu trúc, tự hỏi: (1) thời gian nào, (2) sự thật hay giả định, (3) ai làm hành động, (4) điều gì cần nhấn mạnh?
Hãy giải thích lại bằng tiếng Việt hai câu mẫu phía trên, sau đó tự viết hai câu khác bằng tiếng Anh.

## C. Grammar practice — Bài tập ngữ pháp

**Quy tắc:** Chưa xem Answer Key. Trong chat, GPT giao **3–5 câu mỗi lượt** rồi chờ câu trả lời. **12 câu (8 điền từ + 4 sửa lỗi)**.

### C1. Fill in the blanks — Điền từ / chia động từ (8 câu)

1. The app crashed ___ it ran out of memory.
2. The app crashed because ___ a memory error.
3. We added logs so ___ we could diagnose the issue.
4. The server was overloaded. ___, requests were slow.
5. We delayed the release due ___ a critical issue.
6. The bug appeared ___ the configuration was wrong.
7. We collected screenshots ___ help QA.
8. The timeout happened as a result ___ a network problem.

### C2. Error correction — Sửa lỗi (4 câu)

1. *The app crashed because of it ran out of memory.*
2. *We wrote logs for find the bug.*
3. *Due to the service was down, we stopped.*
4. *It was slow, because of heavy traffic was arriving.*

## D. Vocabulary practice — Ôn từ vựng

**D1. Viết từ/cụm tiếng Anh phù hợp với nghĩa tiếng Việt (5 câu).** Không nhìn bảng A trước khi làm.

1. tái hiện → ______
2. dấu vết ngăn xếp → ______
3. nhật ký → ______
4. ngoại lệ → ______
5. mức độ nghiêm trọng → ______

**D2. Apply the words — Ứng dụng:** Chọn 3 từ bất kỳ trong bảng A, viết một đoạn hội thoại 5 lượt có chứa 3 từ đó. Viết thêm 2 câu với từ mới chưa dùng.

## E. Reading — Đọc hiểu

**Cách học:** Đọc lượt một lấy ý chính; đọc lượt hai tìm chứng cứ. Không dịch từng từ theo thói quen.

> A customer reported that the mobile app sometimes crashed when uploading a photo. The bug appeared only on older Android devices, so the developer could not reproduce it on his own phone. He asked the customer for the app version, device model and approximate time of the crash. The logs showed a memory error after a very large photo was selected. Because the app loaded the full image before resizing it, memory usage increased sharply. The team created a fix that resized images earlier and added a test using large files. They also provided a temporary workaround: customers could upload smaller photos until the update was released.

### Reading questions — Trả lời bằng tiếng Anh (5 câu)

1. When did the crash happen?
2. Which devices were affected?
3. What extra details did the developer request?
4. What caused the memory problem?
5. What temporary workaround was offered?

**Reading extension:** Tóm tắt đoạn đọc bằng 3 câu tiếng Anh. Dùng ít nhất hai từ mới của Unit.

## F. Writing — Bài viết có hướng dẫn

**Writing assignment:** Write a bug report with steps to reproduce, expected versus actual behavior, severity, evidence and a possible workaround.

**Trước khi viết:** lập dàn ý bằng 3–5 gạch đầu dòng; kiểm tra thì/cấu trúc, từ nối và sự rõ ràng.

**Rubric (100 điểm):** Nội dung & nhiệm vụ 40; ngữ pháp trọng tâm 35; từ vựng, mạch lạc và giọng điệu 25. GPT chỉ cung cấp bản sửa sau khi bạn đã nộp.

## G. Speaking & Listening — Voice và văn bản

### G1. Warm-up & pronunciation

- Chọn 5 mục từ trong bảng A: nghe GPT đọc từng mục và lặp lại nếu Voice có phát âm thanh.
- Với mỗi từ, đọc một câu chứa từ đó; giáo viên giải thích trọng âm/cụm nối âm khi cần.
- **Không tự chấm phát âm** nếu GPT chỉ nhận văn bản hoặc transcript.

### G2. Partner role-play / Hội thoại tình huống

- **Role for GPT (English):** You are QA reporting an intermittent crash. I investigate using precise questions and explain likely causes.
- **Hướng dẫn tiếng Việt:** GPT đóng vai đối thoại, hỏi mỗi lần 1 câu và chờ học viên trả lời; chỉ sửa lỗi cản trở hiểu trong lúc nói, còn lại tổng kết ở cuối.
- **Text Mode:** Thực hành dưới dạng chat, chấm độ rõ và ngữ pháp; **không tính** phát âm hoặc nghe.
- **Voice Mode:** Giữ 80–90% đối thoại bằng tiếng Anh; giải thích tiếng Việt theo yêu cầu.

### G3. Listening check — Nghe hiểu qua Voice

- Đề nghị GPT nói **một thông điệp mới 60–90 từ** theo chủ đề này, không hiện bản chép trước khi nghe; sau đó trả lời ba câu về **vấn đề / hành động / bước tiếp theo**.
- GPT có thể dựa vào tình huống gợi ý trong Answer Key để tạo nội dung mới, **không đọc nguyên bài Reading** đã thấy.
- Chỉ ghi `Listening: đã kiểm tra` khi người học **thực sự nghe âm thanh** trước khi thấy chữ.

**Lệnh Voice:** `Voice Partner Mode, Unit 20. You are QA reporting an intermittent crash. I investigate using precise questions and explain likely causes. Ask one question at a time; give feedback in Vietnamese after the role-play.`

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

