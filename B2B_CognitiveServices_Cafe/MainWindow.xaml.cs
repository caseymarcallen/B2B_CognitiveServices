using B2B_CognitiveServices_Cafe.Model;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;

namespace B2B_CognitiveServices_Cafe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<CoffeeOrder> Orders { get; set; }
        
        public MainWindow()
        {
            Orders = new ObservableCollection<CoffeeOrder>();
            InitializeComponent();
            DataContext = this;
        }
        private async void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Orders.Clear();
            var coffeeOrderResult = await new CognitiveServicesClient().MakeLuisRequest(InputTextBox.Text);
            if (coffeeOrderResult != null)
            {
                foreach (var coffeeOrder in coffeeOrderResult.Order)
                {
                    Orders.Add(coffeeOrder);
                }

                ResultTextBox.Text = coffeeOrderResult.JsonResponse;
            }
        }
    }
}
