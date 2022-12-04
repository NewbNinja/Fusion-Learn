using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InGameMessagesUIHandler : MonoBehaviour
{
    public TextMeshProUGUI[] textMeshProUGUIs;
    Queue messageQueue = new Queue();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnGameMessagesReceived(string message)
    {
        Debug.Log($"{Time.time}: InGameMessagesUIHandler:  {message}");


        // Using a queue we're going to display only the latest 4 messages


        messageQueue.Enqueue(message);      // Queue new message

        if (messageQueue.Count > 4)         // If we have 4 messages in the queue already
            messageQueue.Dequeue();         // Remove the oldest from the queue

        int queueIndex = 0;

        // Loop through the messages in the queue and display them
        foreach (string messageInQueue in messageQueue)
        {
            textMeshProUGUIs[queueIndex].text = messageInQueue;
            queueIndex++;
        }
    }
}
