using TomatoRadar.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace TomatoRadar.Utils
{
    static internal class NotificationMessageUtils
    {
        public static ObservableCollection<NotificationMessage> NotificationMessageCollection = new();
        public static DataGrid? DataGridNotificationMessages;

        public static void InitializeNotificationMessageDataGrid(DataGrid dataGrid)
        {
            DataGridNotificationMessages = dataGrid;
        }

        //this return value does nothing, just for the switch expression to discard
        public static bool CreateMessage(MessageType type, string? message)
        {
            NotificationMessageCollection.Add(new NotificationMessage(DateTimeOffset.Now, type, message!));
            DataGridNotificationMessages!.ScrollIntoView(DataGridNotificationMessages.Items[^1]);
            return true;
        }
    }
}
