# 🎮 UDM_16 — Game Caro trực tuyến

Một dự án game Cờ Caro trực tuyến được xây dựng dựa trên kiến trúc Client-Server sử dụng Socket trong .NET. Hệ thống hỗ trợ đa luồng cho phép nhiều trận đấu diễn ra đồng thời, tích hợp các tính năng thách đấu, quản lý thời gian thực, hệ thống khán giả (spectator) và khả năng phục hồi kết nối.

---
## 👥 Thành viên nhóm (Group 03)

| STT | Họ và Tên | 
| :---: | :--- | 
| 1 |  Lê Xuân Vỹ (Leader)| 
| 2 | Nguyễn Ngọc Minh Tuấn | 
| 3 | Trần Ngọc Thái An | 
| 4 | Lê Minh Tân | 
| 5 | Huỳnh Thành Phát | 
| 6 | Nguyễn Trung Kiên |

## ✨ Tính năng nổi bật

### 👥 Quản lý người chơi & Thách đấu
*   **Danh sách Online:** Client kết nối thành công với Server sẽ nhận và hiển thị danh sách các người chơi đang trực tuyến theo thời gian thực.
*   **Hệ thống thách đấu:** Người chơi có thể gửi lời mời thách đấu đến một người dùng cụ thể. Người nhận có quyền **Chấp nhận** hoặc **Từ chối** lời mời.
*   **Phục hồi kết nối (Reconnection):** Nếu bị ngắt kết nối đột ngột, người chơi có thể kết nối lại và tiếp tục trận đấu trong một khoảng thời gian cho phép mà không bị xử thua ngay lập tức.

### ⚔️ Hệ thống Trận đấu & Gameplay
*   **Đa luồng & Quản lý phòng:** Server hỗ trợ nhiều trận đấu diễn ra cùng lúc, mỗi trận được cô lập trong một "phòng" với trạng thái quản lý độc lập.
*   **Xử lý logic tập trung (Server-Authoritative):** Mọi nước đi đều được Server kiểm tra tính hợp lệ. Server chịu trách nhiệm phân định kết quả Thắng - Thua - Hòa để chống gian lận.
*   **Đồng hồ đếm ngược:** Giới hạn thời gian suy nghĩ cho mỗi lượt đi. Nếu hết thời gian mà chưa đánh, Server sẽ tự động xử lý kết quả (xử thua/bỏ lượt) theo luật chơi đã được quy định.
*   **Lưu trữ lịch sử:** Toàn bộ lịch sử các nước đi và kết quả chung cuộc của các trận đấu đều được Server lưu lại để tra cứu và phục vụ tính năng khán giả.

### 👁️ Chế độ Khán giả (Spectator Mode)
*   **Theo dõi giải đấu:** Bất kỳ người dùng nào cũng có thể xem danh sách các trận đấu đang diễn ra và chọn tham gia vào phòng với tư cách là khán giả.
*   **Đồng bộ trạng thái thông minh:** Khán giả tham gia vào giữa trận sẽ được Server gửi ngay lập tức **toàn bộ trạng thái bàn cờ hiện tại** và thời gian đếm ngược. Sau đó, khán giả sẽ tiếp tục nhận các bản cập nhật theo thời gian thực.
*   **Phân quyền chặt chẽ:** Server phân biệt rõ ràng vai trò giữa Người chơi (Player) và Khán giả (Spectator). Khán giả bị vô hiệu hóa quyền gửi nước đi hay thay đổi trạng thái trận đấu.
*   **Thoát an toàn:** Khán giả có thể rời phòng xem bất cứ lúc nào mà không gây ra bất kỳ gián đoạn hay ảnh hưởng nào đến trận đấu của 2 người chơi chính.

---

## 🛠️ Công nghệ sử dụng
*   **Ngôn ngữ:** C#
*   **Framework:** .NET 
*   **Kiến trúc:** Clean Architecture, Domain-Driven Design (DDD)
*   **Giao thức mạng:** TCP/IP Sockets (UDP tùy trường hợp)
*   **Xử lý bất đồng bộ:** `async/await`, `Task` quản lý kết nối đồng thời.

---

## 🚀 Cấu trúc dự án (Architecture)

Solution `GameCaroSocket.slnx` được phân chia rõ ràng theo nguyên tắc Clean Architecture, đảm bảo tính tách biệt, dễ bảo trì và mở rộng:

*   **`CaroGame.Domain`**: Chứa các thực thể cốt lõi (Entities), Value Objects và các quy tắc nghiệp vụ (Game Rules) độc lập với mọi framework bên ngoài.
*   **`CaroGame.Application`**: Chứa các Use Cases, Interface và luồng xử lý chính của trò chơi (Matchmaking, xác thực nước đi).
*   **`CaroGame.Infrastructure`**: Đảm nhiệm việc triển khai các kết nối ra bên ngoài, thao tác với cơ sở dữ liệu (lưu lịch sử) và các dịch vụ hạ tầng.
*   **`CaroGame.Server`**: Đóng vai trò là Presentation layer cho phía Server (Host). Lắng nghe kết nối Socket, quản lý các Session/Room và điều phối dữ liệu sử dụng các logic từ tầng Application.
*   **`GameCaroSocket.Client`**: Ứng dụng phía người dùng. Đảm nhận việc hiển thị giao diện UI, gửi yêu cầu lên Server và cập nhật trạng thái bàn cờ theo thời gian thực.

---
## ⚙️ Yêu cầu hệ thống & Hướng dẫn cài đặt

> 🚧 **Đang cập nhật...**
> 
> Dự án hiện vẫn đang trong giai đoạn phát triển và hoàn thiện. Các thông tin chi tiết về yêu cầu môi trường, cấu hình hệ thống cũng như hướng dẫn từng bước để khởi chạy Server và Client sẽ được nhóm cập nhật đầy đủ tại đây ngay sau khi dự án hoàn thành.
