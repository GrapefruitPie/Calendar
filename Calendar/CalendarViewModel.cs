using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;

namespace Calendar
{
    public struct Holiday
    {
        public string HolidayTitle { get; set; }
        public string Date { get; set; }
        public int days;
        public Holiday(string cake, string pie)
        {
            HolidayTitle = cake;
            if (pie.Split('.').Length > 2) pie = $"{pie.Split('.')[0]}.{pie.Split('.')[1]}";
            Date = pie.Replace(".0", ".");
            if (Date[0] == '0') Date = Date.Substring(1);
            try
            {
                days = int.Parse(pie.Split('.')[0]) + int.Parse(pie.Split('.')[1]) * 30;
            }
            catch
            {
                days = 0;
            }
        }
    }

    class CalendarViewModel : INotifyPropertyChanged
    {
        private List<Holiday> _holidays;
        public List<Holiday> Holidays
        {
            get { return _holidays; }
            set
            {
                _holidays = value;
                OnPropertyChanged("Holidays");
            }
        }

        private string _userid;
        public string UserID
        {
            get { return _userid; }
            set
            {
                _userid = value;
                OnPropertyChanged("UserID");
            }
        }

        public CakeCommand LoadFriends { get; set; }

        public CalendarViewModel()
        {
            //UserID = "255374472";
            LoadFriends = new CakeCommand();
            LoadFriends.Cake = DoLoad;
            Holidays = new List<Holiday>();
            SqlConnectionStringBuilder cstr = new SqlConnectionStringBuilder();
            cstr.IntegratedSecurity = true;
            cstr.DataSource = Environment.MachineName + "\\SQLEXPRESS";
            cstr.InitialCatalog = "Holidays";
            using (SqlConnection db = new SqlConnection(cstr.ConnectionString))
            {
                SqlCommand query = new SqlCommand("select title, [date] from Holiday", db);
                SqlDataReader reader;
                try
                {
                    db.Open();
                    reader = query.ExecuteReader();
                    SqlDataAdapter adapter = new SqlDataAdapter(query);
                    DataTable table = new DataTable();
                    db.Close();
                    adapter.Fill(table);
                    foreach (DataRow row in table.Rows)
                        Holidays.Add(new Holiday((string)row.ItemArray[0], (string)row.ItemArray[1]));
                    Holidays = Holidays.OrderBy((Holiday h) => { return h.days; }).ToList();
                }
                catch
                {
                    MessageBox.Show("Local database is inaccessible");
                }
            }
        }

        private void DoLoad(object candy)
        {
            //REVIEW: Нет, так валидацию не делают. Я же показывал - её надо делать через ValidationRule
            if (candy == null || !(candy is TextBox)) return;
            TextBox idBox = (TextBox)candy;
            if (Validation.GetHasError(idBox))
            {
                MessageBox.Show(Validation.GetErrors(idBox)[0].ErrorContent.ToString());
                return;
            }

            string Friends;
            try
            {
                WebRequest request = WebRequest.Create($"https://api.vk.com/method/friends.get?user_id={idBox.Text}&fields=bdate&name_case=nom&v=5.69");
                WebResponse response = request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    Friends = reader.ReadToEnd();
            }
            catch
            {
                MessageBox.Show("Connection error");
                return;
            }
            //REVIEW: Ну, было бы неплохо здесь рассказать, почему именно return. Написать куда-то или MessageBox
            if (Friends == null) return;
            //REVIEW: А если при десериализации вылетит исключение?
            VKfriends cherry = JsonConvert.DeserializeObject<VKfriends>(Friends);
            if (cherry.error != null)
            {
                MessageBox.Show(cherry.error.error_msg);
                return;
            }
            List<Holiday> hold = new List<Holiday>(Holidays);
            foreach (Item i in cherry.response.items)
            {
                if (i.bdate == null) continue;
                hold.Add(new Holiday($"{i.first_name} {i.last_name}", i.bdate));
            }
            hold = hold.OrderBy((Holiday h) => { return h.days; }).ToList();
            Holidays = hold;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string NewProp)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(NewProp));
        }
    }

    public class CakeCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public Predicate<object> CanExecuteFunc { get; set; }
        public Action<object> Cake { get; set; }
        public bool CanExecute(object parameter) { return true; }
        public void Execute(object parameter)
        {
            Cake(parameter);
        }
    }
}
