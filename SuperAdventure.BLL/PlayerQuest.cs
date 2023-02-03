using System.ComponentModel;

namespace SuperAdventure.BLL
{
    public class PlayerQuest : INotifyPropertyChanged
    {
        public PlayerQuest(Quest details)
        {
            Details = details;
            IsCompleted = false;
        }

        private Quest _details;
        private bool _isCompleted;
        public Quest Details
        {
            get { return _details; }
            set
            {
                _details = value;
                OnPropertyChanged("Details");
            }
        }
        public bool IsCompleted
        {
            get { return _isCompleted; }
            set
            {
                _isCompleted = value;
                OnPropertyChanged("IsCompleted");
                OnPropertyChanged("Name");
            }
        }
        public string Name
        {
            get { return Details.Name; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
