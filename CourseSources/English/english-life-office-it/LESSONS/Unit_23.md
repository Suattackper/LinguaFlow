# UNIT 23 — Deployment and Incidents

**Chủ đề tiếng Việt:** Triển khai và sự cố hệ thống  
**Trình độ dự kiến:** B1+ | IT  
**Thời lượng gợi ý:** 6 buổi × 60 phút; có thể học chậm hơn nếu cần  
**Đáp án riêng:** `ANSWER_KEYS/Unit_23_Answers.md`  
**Chế độ:** Text Teacher / Voice Teacher / Voice Partner / Mixed

## Mục tiêu / Learning outcomes

- Sử dụng **20 từ/cụm từ** thuộc chủ đề **triển khai và sự cố hệ thống** và tự đặt câu có ngữ cảnh. 
- Phân biệt và dùng đúng **Extended passive forms** và **Incident sequencing and contrast**.
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
| 1 | **deploy** /dɪplˈɔɪ/ | triển khai | A hotfix was tested in staging, reviewed and deployed the next morning. |
| 2 | **rollout** /ɹˈəʊlaʊt/ | phát hành theo giai đoạn | Đặt 1 câu của riêng bạn với từ này. |
| 3 | **rollback** /ɹˈəʊlbak/ | quay lại phiên bản trước | Đặt 1 câu của riêng bạn với từ này. |
| 4 | **hotfix** /hˈɒtfɪks/ | bản sửa khẩn | A hotfix was tested in staging, reviewed and deployed the next morning. |
| 5 | **incident** /ˈɪnsɪdənt/ | sự cố | The incident was stressful, but the response process was clear. |
| 6 | **monitoring** /mˈɒnɪtəɹɪŋ/ | giám sát | Monitoring showed an unexpected increase in payment errors. |
| 7 | **alert** /ɐlˈɜːt/ | cảnh báo | An alert was triggered, and the on-call engineer checked the latest deployment. |
| 8 | **uptime** /ˈʌptaɪm/ | thời gian hoạt động | Đặt 1 câu của riêng bạn với từ này. |
| 9 | **downtime** /dˈaʊntaɪm/ | thời gian gián đoạn | Đặt 1 câu của riêng bạn với từ này. |
| 10 | **service** /sˈɜːvɪs/ | dịch vụ | After the service stabilized, they wrote a postmortem explaining what happened, how customers were affected and which safeguards should be added. |
| 11 | **recovery** /ɹɪkˈʌvəɹi/ | khôi phục | Đặt 1 câu của riêng bạn với từ này. |
| 12 | **root cause analysis** /ɹˈuːt kˈɔːz ɐnˈaləsˌɪs/ | phân tích nguyên nhân gốc | Đặt 1 câu của riêng bạn với từ này. |
| 13 | **postmortem** /pˈəʊstmɔːtəm/ | báo cáo hậu kiểm | After the service stabilized, they wrote a postmortem explaining what happened, how customers were affected and which safeguards should be added. |
| 14 | **availability** /ɐvˌeɪləbˈɪlɪti/ | tính sẵn sàng | Đặt 1 câu của riêng bạn với từ này. |
| 15 | **traffic** /tɹˈafɪk/ | lưu lượng | The team then increased traffic gradually while monitoring the error rate. |
| 16 | **health check** /hˈɛlθ tʃˈɛk/ | kiểm tra tình trạng | Đặt 1 câu của riêng bạn với từ này. |
| 17 | **feature flag** /fˈiːtʃə flˈaɡ/ | cờ bật tắt tính năng | The feature flag was turned off while the team investigated. |
| 18 | **release notes** /ɹɪlˈiːs nˈəʊts/ | ghi chú phát hành | Đặt 1 câu của riêng bạn với từ này. |
| 19 | **canary release** /kənˈeəɹi ɹɪlˈiːs/ | phát hành thử cho nhóm nhỏ | Đặt 1 câu của riêng bạn với từ này. |
| 20 | **on-call** /ˌɒnkˈɔːl/ | trực xử lý sự cố | An alert was triggered, and the on-call engineer checked the latest deployment. |

**Luyện từ / Vocabulary production:** Viết 8 câu: 3 câu đời sống, 3 câu công sở, 2 câu IT. Dùng từ của Unit và tự kiểm tra nghĩa, loại từ, cách phối hợp từ.

## B. Grammar — Giải thích song ngữ

### B1. Extended passive forms

- **Công thức / Form:** `has/have been + V3; will be + V3`
- **Cách dùng (tiếng Việt):** Diễn tả thay đổi đã được thực hiện hoặc sẽ được thực hiện mà chủ thể không quan trọng.
- **Example:** *The patch has been deployed to production.*
- **Nghĩa:** Bản vá đã được triển khai lên production.
- **Lỗi dễ mắc:** Không nói has been deploy; dùng deployed.

### B2. Incident sequencing and contrast

- **Công thức / Form:** `initially / meanwhile / however / after that / eventually`
- **Cách dùng (tiếng Việt):** Đưa mốc thời gian và tương phản để báo cáo sự cố mạch lạc, tránh nhảy ý.
- **Example:** *Initially, errors increased; however, the rollback stabilized traffic.*
- **Nghĩa:** Ban đầu lỗi tăng; tuy nhiên rollback đã ổn định lưu lượng.
- **Lỗi dễ mắc:** However thường nối hai mệnh đề/câu có dấu câu rõ ràng.

### B3. So sánh và tự kiểm tra

Trước khi chọn cấu trúc, tự hỏi: (1) thời gian nào, (2) sự thật hay giả định, (3) ai làm hành động, (4) điều gì cần nhấn mạnh?
Hãy giải thích lại bằng tiếng Việt hai câu mẫu phía trên, sau đó tự viết hai câu khác bằng tiếng Anh.

## C. Grammar practice — Bài tập ngữ pháp

**Quy tắc:** Chưa xem Answer Key. Trong chat, GPT giao **3–5 câu mỗi lượt** rồi chờ câu trả lời. **12 câu (8 điền từ + 4 sửa lỗi)**.

### C1. Fill in the blanks — Điền từ / chia động từ (8 câu)

1. The hotfix ___ (already / deploy).
2. The next release ___ (test) tomorrow.
3. The feature flag ___ (turn) off yesterday.
4. The incident report ___ (send) to stakeholders next week.
5. The issue ___ (not / resolve) yet.
6. Errors increased; ___, users could still log in.
7. First, we checked the logs. ___, we rolled back.
8. New alerts ___ (configure) this morning.

### C2. Error correction — Sửa lỗi (4 câu)

1. *The patch has been deploy.*
2. *The service will be restart tonight.*
3. *The error was resolved, however it returned later.*
4. *The alerts has been configured.*

## D. Vocabulary practice — Ôn từ vựng

**D1. Viết từ/cụm tiếng Anh phù hợp với nghĩa tiếng Việt (5 câu).** Không nhìn bảng A trước khi làm.

1. triển khai → ______
2. phát hành theo giai đoạn → ______
3. quay lại phiên bản trước → ______
4. bản sửa khẩn → ______
5. sự cố → ______

**D2. Apply the words — Ứng dụng:** Chọn 3 từ bất kỳ trong bảng A, viết một đoạn hội thoại 5 lượt có chứa 3 từ đó. Viết thêm 2 câu với từ mới chưa dùng.

## E. Reading — Đọc hiểu

**Cách học:** Đọc lượt một lấy ý chính; đọc lượt hai tìm chứng cứ. Không dịch từng từ theo thói quen.

> The team released a new checkout flow to ten percent of users on Tuesday. Monitoring showed an unexpected increase in payment errors. An alert was triggered, and the on-call engineer checked the latest deployment. The feature flag was turned off while the team investigated. They found that an error message from an external provider was not being handled correctly. A hotfix was tested in staging, reviewed and deployed the next morning. The team then increased traffic gradually while monitoring the error rate. After the service stabilized, they wrote a postmortem explaining what happened, how customers were affected and which safeguards should be added. The incident was stressful, but the response process was clear.

### Reading questions — Trả lời bằng tiếng Anh (5 câu)

1. What percentage of users received the first rollout?
2. What problem did monitoring show?
3. What immediate action was taken?
4. When was the hotfix deployed?
5. What did the postmortem describe?

**Reading extension:** Tóm tắt đoạn đọc bằng 3 câu tiếng Anh. Dùng ít nhất hai từ mới của Unit.

## F. Writing — Bài viết có hướng dẫn

**Writing assignment:** Write a timeline of an incident using passive constructions and time connectors, followed by three prevention actions.

**Trước khi viết:** lập dàn ý bằng 3–5 gạch đầu dòng; kiểm tra thì/cấu trúc, từ nối và sự rõ ràng.

**Rubric (100 điểm):** Nội dung & nhiệm vụ 40; ngữ pháp trọng tâm 35; từ vựng, mạch lạc và giọng điệu 25. GPT chỉ cung cấp bản sửa sau khi bạn đã nộp.

## G. Speaking & Listening — Voice và văn bản

### G1. Warm-up & pronunciation

- Chọn 5 mục từ trong bảng A: nghe GPT đọc từng mục và lặp lại nếu Voice có phát âm thanh.
- Với mỗi từ, đọc một câu chứa từ đó; giáo viên giải thích trọng âm/cụm nối âm khi cần.
- **Không tự chấm phát âm** nếu GPT chỉ nhận văn bản hoặc transcript.

### G2. Partner role-play / Hội thoại tình huống

- **Role for GPT (English):** You are the on-call engineer. Give an incident update to a worried customer without exposing unnecessary technical details.
- **Hướng dẫn tiếng Việt:** GPT đóng vai đối thoại, hỏi mỗi lần 1 câu và chờ học viên trả lời; chỉ sửa lỗi cản trở hiểu trong lúc nói, còn lại tổng kết ở cuối.
- **Text Mode:** Thực hành dưới dạng chat, chấm độ rõ và ngữ pháp; **không tính** phát âm hoặc nghe.
- **Voice Mode:** Giữ 80–90% đối thoại bằng tiếng Anh; giải thích tiếng Việt theo yêu cầu.

### G3. Listening check — Nghe hiểu qua Voice

- Đề nghị GPT nói **một thông điệp mới 60–90 từ** theo chủ đề này, không hiện bản chép trước khi nghe; sau đó trả lời ba câu về **vấn đề / hành động / bước tiếp theo**.
- GPT có thể dựa vào tình huống gợi ý trong Answer Key để tạo nội dung mới, **không đọc nguyên bài Reading** đã thấy.
- Chỉ ghi `Listening: đã kiểm tra` khi người học **thực sự nghe âm thanh** trước khi thấy chữ.

**Lệnh Voice:** `Voice Partner Mode, Unit 23. You are the on-call engineer. Give an incident update to a worried customer without exposing unnecessary technical details. Ask one question at a time; give feedback in Vietnamese after the role-play.`

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

