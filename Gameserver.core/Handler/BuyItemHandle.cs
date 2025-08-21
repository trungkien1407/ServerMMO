using Gameserver.core.Dto;
using Gameserver.core.Manager;
using Gameserver.core.Network;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace Gameserver.core.Handler
{
    public class BuyItemHandle : IMessageHandler
    {
        public string Action => "buy_item";

        private readonly SessionManager _sessionManager;
        private readonly ItemDataManager _itemDataManager;

        public BuyItemHandle(SessionManager sessionManager, ItemDataManager itemDataManager)
        {
            _sessionManager = sessionManager;
            _itemDataManager = itemDataManager;
        }

        public async Task Handle(Guid clientId, JObject data, WatsonWsServer server)
        {
            // 1. Lấy session của người chơi
            var session = _sessionManager.GetSessionByClientId(clientId);
            if (session == null || session.Character == null)
            {
                // Không tìm thấy session hoặc nhân vật, không xử lý
                return;
            }

            // 2. Phân tích dữ liệu từ client
            int itemID = data["itemID"]?.Value<int>() ?? 0;
            int quantity = data["quantity"]?.Value<int>() ?? 1; // Mặc định là 1 nếu client không gửi

            if (itemID == 0 || quantity <= 0)
            {
                // Dữ liệu không hợp lệ
                return;
            }

            // 3. Lấy thông tin vật phẩm để xác nhận nó tồn tại
            var itemData = _itemDataManager.GetItemById(itemID);
            if (itemData == null)
            {
                // Vật phẩm không tồn tại trong hệ thống, không làm gì cả
                Console.WriteLine($"[BuyItemHandle] Client {clientId} tried to buy non-existent item ID: {itemID}");
                return;
            }

            // 4. Bỏ qua logic tiền tệ (đã được comment out)

            // 5. Thêm vật phẩm vào túi đồ của người chơi
            // Phương thức này sẽ xử lý logic trên cache (bộ nhớ)
            // và tự động gửi cập nhật UI cho client.
            await _sessionManager.AddItemToInventory(session, itemID, quantity);

            // 6. Bỏ qua logic hoàn tiền (vì không có giao dịch tiền tệ)

            // 7. KHÔNG gọi SaveData() trực tiếp.
            // Mọi thay đổi trong cache (túi đồ) sẽ được lưu bởi hệ thống Autosave định kỳ.
            // await _sessionManager.SaveData(clientId); // <--- DÒNG NÀY ĐƯỢC XÓA ĐI

            // 8. KHÔNG cần gửi lại thông tin nhân vật vì không có gì thay đổi (tiền, v.v.)
            // Việc cập nhật túi đồ đã được thực hiện bên trong AddItemToInventory.
            // Nếu sau này bạn thêm lại logic tiền tệ, bạn có thể mở lại phần này.
            /*
            var updateCharMsg = new BaseMessage
            {
                Action = "update_character_info",
                Data = JObject.FromObject(new
                {
                    gold = session.Character.Gold
                })
            };
            await server.SendAsync(clientId, Newtonsoft.Json.JsonConvert.SerializeObject(updateCharMsg));
            */
        }
    }
}