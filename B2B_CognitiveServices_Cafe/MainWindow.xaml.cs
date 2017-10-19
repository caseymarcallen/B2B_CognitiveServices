using B2B_CognitiveServices_Cafe.Model;
using Microsoft.CognitiveServices.SpeechRecognition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace B2B_CognitiveServices_Cafe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<CoffeeOrder> Orders { get; set; }

        private string inputText;
        public string InputText
        {
            get => inputText;
            set { inputText = value; OnPropertyChanged<string>(); }
        }


        public MainWindow()
        {
            Orders = new ObservableCollection<CoffeeOrder>();
            InitializeComponent();
            DataContext = this;
        }
        private void OnPropertyChanged<T>([CallerMemberName]string caller = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        private async void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Orders.Clear();
            var coffeeOrderResult = await MakeLuisRequest(InputTextBox.Text);
            UpdateCoffeeOrder(coffeeOrderResult);
        }

        private void UpdateCoffeeOrder(CoffeeOrderResult coffeeOrderResult)
        {
            if (coffeeOrderResult != null)
            {
                Dispatcher.Invoke(() =>
                {
                    Orders.Clear();
                    InputText = string.Empty;
                    foreach (var coffeeOrder in coffeeOrderResult.Order)
                    {
                        Orders.Add(coffeeOrder);
                    }
                    ResultTextBox.Text = coffeeOrderResult.JsonResponse;
                });
            }
        }









        //--------------------------------------------------------------------
        // 1 - LUIS


        // --------------------------------------
        // Edit these values
        // --------------------------------------
        private const string LuisAppId = "b2cc5cf4-4129-4daa-a6c6-97dbcac22121";
        private const string LuisSubscriptionKey = "7dbea38a27424387b2b3563b4c80ad72";




        public async Task<CoffeeOrderResult> MakeLuisRequest(string orderText)
        {
            string apiResponse = "";

            // INSERT LUIS API CALL CODE HERE!
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", LuisSubscriptionKey);
            var uri = $"https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{LuisAppId}?spellCheck=true&q={orderText}";
            var response = await client.GetAsync(uri);
            apiResponse = await response.Content.ReadAsStringAsync();


            var result = JsonConvert.DeserializeObject<LuisModel>(apiResponse);

            var order = ProcessOrder(result);

            return new CoffeeOrderResult
            {
                JsonResponse = apiResponse,
                Order = order
            };
        }

        private IEnumerable<CoffeeOrder> ProcessOrder(LuisModel result)
        {
            var orders = new List<CoffeeOrder>();
            try
            {
                foreach (var compositeEntity in result.compositeEntities)
                {
                    try
                    {
                        var coffeeOrder = compositeEntity.ToCoffeeOrder(result.entities);
                        if (coffeeOrder != null)
                        {
                            orders.Add(coffeeOrder);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Caught an error processing compositeEntity: " + e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an error processing LuisModel: " + e);
            }

            return orders;
        }




        //--------------------------------------------------------------------
        // 2 - Speech Recognition


        // --------------------------------------
        // Edit these values
        // --------------------------------------
        private const string BingSpeechSubscriptionKey = "4a6aaf9b71704e8192b9bff951db82c2";


        private MicrophoneRecognitionClient micClient;


        private void ListenToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (micClient == null)
            {
                CreateMicrophoneClientWithIntent();
            }

            micClient.StartMicAndRecognition();
        }


        private void CreateMicrophoneClientWithIntent()
        {
            micClient =
                SpeechRecognitionServiceFactory.CreateMicrophoneClientWithIntent(
                "en-AU",
                BingSpeechSubscriptionKey,
                LuisAppId,
                LuisSubscriptionKey);
            
            micClient.OnIntent += OnIntentHandler;

            // Event handlers for speech recognition results
            micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                InputText = e.PartialResult;
            });
        }
        
        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                InputText += $"    ERROR: {e.SpeechErrorText}";
            });
            
        }

        private void OnIntentHandler(object sender, SpeechIntentEventArgs e)
        {
            // Stop the microphone
            micClient.EndMicAndRecognition();

            string apiResponse = e.Payload;

            var result = JsonConvert.DeserializeObject<LuisModel>(apiResponse);

            var order = ProcessOrder(result);

            UpdateCoffeeOrder(new CoffeeOrderResult
            {
                JsonResponse = apiResponse,
                Order = order
            });
        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Recording)
                {
                    InputText = "Listening...";
                }
                else
                {
                    InputText += " Placing Order...";
                }
            });
        }

    }
}
