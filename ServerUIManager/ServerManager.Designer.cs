namespace ServerUIManager
{
    partial class ServerManager
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            StartServer = new Button();
            StopServer = new Button();
            BanPlayer = new Button();
            UnBanPlayer = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            Update = new Button();
            numericUpDown1 = new NumericUpDown();
            label2 = new Label();
            message = new Label();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            SuspendLayout();
            // 
            // StartServer
            // 
            StartServer.Location = new Point(122, 52);
            StartServer.Name = "StartServer";
            StartServer.Size = new Size(220, 52);
            StartServer.TabIndex = 0;
            StartServer.Text = "StartServer";
            StartServer.UseVisualStyleBackColor = true;
            StartServer.Click += StartServer_Click;
            // 
            // StopServer
            // 
            StopServer.Location = new Point(582, 52);
            StopServer.Name = "StopServer";
            StopServer.Size = new Size(232, 52);
            StopServer.TabIndex = 1;
            StopServer.Text = "StopServer";
            StopServer.UseVisualStyleBackColor = true;
            StopServer.Click += StopServer_Click;
            // 
            // BanPlayer
            // 
            BanPlayer.Location = new Point(83, 449);
            BanPlayer.Name = "BanPlayer";
            BanPlayer.Size = new Size(169, 52);
            BanPlayer.TabIndex = 2;
            BanPlayer.Text = "BanPlayer";
            BanPlayer.UseVisualStyleBackColor = true;
            // 
            // UnBanPlayer
            // 
            UnBanPlayer.Location = new Point(357, 449);
            UnBanPlayer.Name = "UnBanPlayer";
            UnBanPlayer.Size = new Size(217, 52);
            UnBanPlayer.TabIndex = 3;
            UnBanPlayer.Text = "UnBanPlayer";
            UnBanPlayer.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(291, 350);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(604, 43);
            textBox1.TabIndex = 5;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(61, 350);
            label1.Name = "label1";
            label1.Size = new Size(175, 37);
            label1.TabIndex = 6;
            label1.Text = "Tên Nhân Vật";
            // 
            // Update
            // 
            Update.Location = new Point(73, 248);
            Update.Name = "Update";
            Update.Size = new Size(220, 52);
            Update.TabIndex = 7;
            Update.Text = "UpDate";
            Update.UseVisualStyleBackColor = true;
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(291, 156);
            numericUpDown1.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(270, 43);
            numericUpDown1.TabIndex = 8;
            numericUpDown1.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(61, 158);
            label2.Name = "label2";
            label2.Size = new Size(217, 37);
            label2.TabIndex = 9;
            label2.Text = "Tỉ Lệ Exp/ Rơi đồ";
            // 
            // message
            // 
            message.AutoSize = true;
            message.Location = new Point(133, 572);
            message.Name = "message";
            message.Size = new Size(0, 37);
            message.TabIndex = 10;
            // 
            // ServerManager
            // 
            AutoScaleDimensions = new SizeF(15F, 37F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1010, 618);
            Controls.Add(message);
            Controls.Add(label2);
            Controls.Add(numericUpDown1);
            Controls.Add(Update);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(UnBanPlayer);
            Controls.Add(BanPlayer);
            Controls.Add(StopServer);
            Controls.Add(StartServer);
            Name = "ServerManager";
            Text = "ServerManager";
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button StartServer;
        private Button StopServer;
        private Button BanPlayer;
        private Button UnBanPlayer;
        private TextBox textBox1;
        private Label label1;
        private Button Update;
        private NumericUpDown numericUpDown1;
        private Label label2;
        private Label message;
    }
}
