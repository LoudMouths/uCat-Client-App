using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MText;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System.Linq;
using System.IO;
using Meta.WitAi.TTS.Utilities;
using CandyCoded.env;
using UnityEngine.EventSystems;
using EListeningState = WitListeningStateManager.ListeningState;

public class ConversationManager : MonoBehaviour
{
    public TTSSpeaker _uCatSpeaker;
    public TTSSpeaker _userSpeaker;

    private AudioSource uCatAudioSource;

    
    private OpenAIAPI api;
    private List<ChatMessage> messages;
    private Coroutine submitEvery5s;

    private UIManager _uiManager;
    private WitListeningStateManager _witListeningStateManager;
    private LevelManager _levelManager;

    public bool uCatHasStartedSpeaking;
    public float uCatResponseTimeoutLimit;
    public float uCatResponseTimeout;

    private AnimationDriver uCatAnimationDriver;
    public string standardInitializationMessage;
    public string advancedInitializationMessage;

    private Modular3DText subtitleText;
    private Modular3DText uCatSpeechText;

    public string userSpeechText;

    [System.Serializable]
    public class Secrets
    {
        public string OPENAI_API_KEY;
    }

    public delegate void ApiSubmissionEvent(string textToSubmit);
    public static event ApiSubmissionEvent OnUserFinishedSpeaking;

    private void OnEnable()
    {
        OnUserFinishedSpeaking += GetOpenAIResponse;
    }

    private void OnDisable()
    {
        OnUserFinishedSpeaking -= GetOpenAIResponse;
    }
    void Start(){
        // Assign scripts
        _uiManager = GetComponent<UIManager>();
        _levelManager = GetComponent<LevelManager>();
        _witListeningStateManager = GetComponent<WitListeningStateManager>();
        subtitleText = GameObject.FindWithTag("SubtitleText3D").GetComponent<Modular3DText>();
        uCatSpeechText = GameObject.FindWithTag("uCatSpeechText").GetComponent<Modular3DText>();
        uCatAnimationDriver = GameObject.FindWithTag("uCat").GetComponent<AnimationDriver>();

        _witListeningStateManager.TransitionToState(EListeningState.ListeningForConversationModeInput);

        standardInitializationMessage =  "Your name is 'uCat'. You are a humble, kind-hearted, compassionate, and sassy robocat. Sometimes you say \"meow\" when you speak. You help me learn how to use my implanted brain-computer interfaces to move inside the metaverse. You keep your responses short and to the point.";
        advancedInitializationMessage =  
        "Your name is 'uCat'. You are a humble, kind-hearted, compassionate, and sassy robocat." +
        " Sometimes you say \"meow\" when you speak. You help me learn how to use my implanted brain-computer interfaces to move inside the metaverse." +
        " You keep your responses short and to the point" +
        "." + "Your response to the initial question 'Where am I?' should be `Good question! You spent the whole day fine tuning your Speech BCI and now you're having your first conversation in the metaverse.`" +
        " At the end of each response, categorise your response into one of the following categories: 'happy' 'sad' 'confused' 'neutral' 'cheeky'." +
        " The category should be the last sentence of your response and just consist of the word by itself, e.g., 'Happy.'";
        if (_levelManager.CurrentLevel != "ConvoMode") {
            this.enabled = false; 
            return;
        }

        InitiliazeUcatConversation();

        // Initial dialogue and animation
        uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Happy;
        uCatResponseTimeout = 0;

        uCatAudioSource = GameObject.FindWithTag("uCatConversationAudioSource").GetComponent<AudioSource>();
        
        
    }

    string apiKeyFromJson()
    {
        string filePath = Path.Combine(Application.dataPath, "secrets.json");

        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath);
            Secrets myData = JsonUtility.FromJson<Secrets>(dataAsJson);
            Debug.Log("JSON loaded successfully");
            return myData.OPENAI_API_KEY;
        }
        else
        {
            Debug.LogError("Cannot find JSON file at: " + filePath);
            return null;
        }
    }

    public void HandlePartialSpeech(string text) {
        subtitleText.UpdateText(text);
    }

    public void HandleUserSpeech(string spokenText) {
        // This function is called from FreeSpeechManager when the user speaks (as long as they are allowed to currently)
        uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Confused;
        
        // Assign to the global variable to be used by the event callback
        userSpeechText = spokenText;
        
        _userSpeaker.Speak(spokenText);
        subtitleText.UpdateText(spokenText);
        _witListeningStateManager.TransitionToState(EListeningState.WaitingForConversationResponse);
        
        // Send the text to the server
        //GetOpenAIResponse(spokenText);
    }

    public void UserSpeechAudioHasBeenPlayed()
    {
        subtitleText.UpdateText("");
        OnUserFinishedSpeaking?.Invoke(userSpeechText);
        userSpeechText = "";
    }
   
    private void InitiliazeUcatConversation(){
        api = new OpenAIAPI(apiKeyFromJson());
        messages = new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRole.System, advancedInitializationMessage)
        };
    }


    private async void GetOpenAIResponse(string textToSubmit){

        // Test commit change (safe)
        if( textToSubmit.Length < 1) return;

        // Construct the message object
        ChatMessage userMessage  = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = textToSubmit;
        if(userMessage.Content.Length > 100) userMessage.Content = userMessage.Content.Substring(0,100);

        //Add message to the ongoing conversation
        messages.Add(userMessage);

        //send entire chat to OpenAI to get its response
        uCatHasStartedSpeaking = true;
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest(){
            Model = Model.ChatGPTTurbo,
            Temperature = 0.7,
            MaxTokens = 50,
            Messages = messages
        });
        
        uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Idle;

        // TODO handle no response error here, make uCat say 'something went wrong'

        //get OpenAI response
        ChatMessage responseMessage = new ChatMessage();
        string response = chatResult.Choices[0].Message.Content;

        // This object will be added to the total messages
        responseMessage.Role = chatResult.Choices[0].Message.Role;
        responseMessage.Content = response;

        if (response.Length > 0) {
            string lastWord = GetLastWordOfLastSentence(response);
            // Get the normal sentence so uCat says it normally.
            string sentenceWithoutEmotion = response.Substring(0, response.Length - (lastWord.Length+1));

            // Play animation based on emotion catewgory
            PlayEmotionAnimation(lastWord);
            //TTS speak uCat's response
            _uCatSpeaker.Speak(sentenceWithoutEmotion);
            uCatSpeechText.UpdateText(sentenceWithoutEmotion);
            uCatHasStartedSpeaking = false;
            uCatResponseTimeout = 0;
            //add the response to the total list of messages
            messages.Add(responseMessage);
        }

        else {
            // Catch it if API has an error somehow
            _witListeningStateManager.TransitionToState(EListeningState.ListeningForConversationModeInput);
        }
    }

    string GetLastWordOfLastSentence(string text)
    {
        // Split the text into sentences
        string[] sentences = text.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Get the last sentence
        string lastSentence = sentences.LastOrDefault()?.Trim();

        if (string.IsNullOrEmpty(lastSentence))
        {
            return string.Empty;
        }

        // Split the last sentence into words
        string[] words = lastSentence.Split(new char[] { ' ', ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);

        // Return the last word
        return words.LastOrDefault();
    }

    

    void PlayEmotionAnimation(string text) {
        // Play the appropriate animation based on the emotion category

        switch (text.ToLower()) {
            case "happy":
                uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Happy;
                break;
            case "sad":
                 uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Sad;
                break;
            case "confused":
                uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Confused;
                break;
            case "neutral":
                uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Idle;
                break;
            case "cheeky":
                uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Peeking;
                break;
            default:
                break;
        }

    }

    public void UcatIsDoneSpeaking() {
        _uCatSpeaker.Stop();
        uCatSpeechText.UpdateText("");
        uCatAnimationDriver.catAnimation = AnimationDriver.CatAnimations.Idle;
        _witListeningStateManager.TransitionToState(EListeningState.ListeningForConversationModeInput);
    }

    void Update() {
        if (uCatHasStartedSpeaking) {
            uCatResponseTimeout += Time.deltaTime;
        }

        if (uCatResponseTimeout > uCatResponseTimeoutLimit) {
                uCatHasStartedSpeaking = false;
                uCatResponseTimeout = 0;
                UcatIsDoneSpeaking();
         }
    }
}