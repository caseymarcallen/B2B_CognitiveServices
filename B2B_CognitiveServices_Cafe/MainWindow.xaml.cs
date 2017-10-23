﻿using B2B_CognitiveServices_Cafe.Model;
using Microsoft.CognitiveServices.SpeechRecognition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
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

                ConfirmOrder(coffeeOrderResult);
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
                //BingSpeechSubscriptionKey,
                "1fe791132e4446fbb34a81416c94ba3d",
                "83cb3abfeac041958f8216c9648fc8da",
                LuisAppId,
                LuisSubscriptionKey,
                "https://54eddef2177945bfa631ea5b31a4a1a4.api.cris.ai/ws/cris/speech/recognize");
            micClient.AuthenticationUri = "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken";

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




        //--------------------------------------------------------------------
        // 2 - Text to Speech Confirmation

        private Random _rand = new Random();
        private Cortana _cortana = new Cortana();

        // --------------------------------------
        // Edit these values
        // --------------------------------------


        private static void PlayAudio(object sender, GenericEventArgs<Stream> args)
        {
            SoundPlayer player = new SoundPlayer(args.EventData);
            player.PlaySync();
            args.EventData.Dispose();
        }

        private static void ErrorHandler(object sender, GenericEventArgs<Exception> e)
        {
            Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
        }


        private string FormatOrderText(CoffeeOrderResult order)
        {
            string orderConfirmation = "";

            if (order.Order.Any())
            {
                var intros = new string[] { "No worries", "Sure", "Good choice" };
                var outros = new string[] { "coming right up", "on the way", ". That won't be long" };

                orderConfirmation = $"{intros[_rand.Next(0, intros.Length)]}. ";
                foreach (var orderItem in order.Order)
                {
                    orderConfirmation += $"{orderItem.Number} {orderItem.CoffeeType}, ";
                }
                orderConfirmation = orderConfirmation.TrimEnd(new[] { ',', ' '}) + $" {outros[_rand.Next(0, outros.Length)]}. ";
            }
            else
            {
                orderConfirmation = "I'm sorry, I didn't quite catch that.";
            }

            return orderConfirmation;
        }

        private void ConfirmOrder(CoffeeOrderResult order)
        {
            Console.WriteLine("Starting Authtentication");
            string accessToken;
            
            Authentication auth = new Authentication(BingSpeechSubscriptionKey);

            try
            {
                accessToken = auth.GetAccessToken();
                Console.WriteLine("Token: {0}\n", accessToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed authentication.");
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
                return;
            }

            Console.WriteLine("Starting TTSSample request code execution.");
            
            var orderConfirmationText = FormatOrderText(order);

            string requestUri = "https://speech.platform.bing.com/synthesize";
            

            _cortana.OnAudioAvailable += PlayAudio;
            _cortana.OnError += ErrorHandler;
            
            _cortana.Speak(CancellationToken.None, new Cortana.InputOptions()
            {
                RequestUri = new Uri(requestUri),
                Text = orderConfirmationText,
                VoiceType = Gender.Female,
                Locale = "en-US",
                VoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)", // en-US knows how to say cappuccino properly (AU does not)
                OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
                AuthorizationToken = "Bearer " + accessToken,
            }).Wait();
        }

    }
}
