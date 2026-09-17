using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PhanMemInAnERP.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void AddLog(string message, bool isError = false)
        {
            // Đây là base method - derived classes có thể override
            // Hoặc có thể thêm logic logging chung ở đây
        }
    }
}