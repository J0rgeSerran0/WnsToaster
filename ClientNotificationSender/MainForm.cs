namespace ClientNotificationSender
{

    using NotificationHub;
    using System;
    using System.Windows.Forms;

    public partial class MainForm : Form
    {
        
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnSendNotification_Click(object sender, EventArgs e)
        {
            try
            {
                var notificationSender = new NotificationSender(this.txtSecret.Text, this.txtSid.Text);

                var notificationModel = new NotificationModel() { ChannelUri = this.txtChannelUri.Text, XmlMessage = this.txtNotification.Text };

                var notificationResponseModel = notificationSender.PostToastToWns(notificationModel);

                MessageBox.Show((notificationResponseModel.StatusCode == System.Net.HttpStatusCode.OK ? "Notification push sent correctly" : "An error ocurred sending the notification push: " + notificationResponseModel.StatusCode.ToString()), "Client Notification Sender");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Client Notification Sender");
            }
        }

    }

}