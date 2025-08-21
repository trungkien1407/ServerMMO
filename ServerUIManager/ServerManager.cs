using Gameserver.core;
namespace ServerUIManager
{
    public partial class ServerManager : Form
    {
        private ServerHostWrapper _serverHost;
        private readonly IServiceProvider _serviceProvider;
        public ServerManager()
        {
            InitializeComponent();
            _serverHost = new ServerHostWrapper();
        }


        private async void StartServer_Click(object sender, EventArgs e)
        {
            if (_serverHost.IsRunning)
            {
                MessageBox.Show("Server đã chạy rồi!");
                return;
            }

            // Vô hiệu hóa nút start để tránh nhấn nhiều lần
            StartServer.Enabled = false;

            bool started = await _serverHost.StartAsync();

            if (started)
            {
                message.Text = "Server is Running";
            }
            else
            {
                message.Text = "Error";
            }

            // Bật lại nút Start để có thể thử lại hoặc dừng/start server sau này
            StartServer.Enabled = true;
            StopServer.Enabled = true;
        }

        private async void StopServer_Click(object sender, EventArgs e)
        {
            StopServer.Enabled = false;
            StartServer.Enabled = false;

            if (_serverHost.IsRunning)
            {
                message.Text = "Đang gửi thông báo bảo trì và chờ 1 phút...";

                await _serverHost.StopAsync(); // Bên trong đã có Task.Delay 1 phút

                message.Text = "Server đã dừng.";
                MessageBox.Show("Server đã dừng hoàn toàn sau khi bảo trì.");
               
            }
            else
            {
                message.Text = "Server chưa chạy.";
            }

            StopServer.Enabled = true;
            StartServer.Enabled = true;
        }
      

    }
}
