using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using TMPro;
using UnityEngine.UI;

public class NetworkChat : NetworkBehaviour
{
	// çook aşırı basit bir sohbet scripti
	// boş bir oyun objesien bağla ve networkIdentiy ekle
	
	// chat şeysine UI dan textmeshpro ekle
	// input da input textmeshpro işte
	// entere basıp mesaj yolla
	
    //simple chat box
    public TextMeshProUGUI chat;

	// input box (press enter to send)
    public TMP_InputField input;

    void Start()
    {
        input.onSubmit.AddListener(sendtext);
    }

    public void sendtext(string g)
    {
        SendText(g, ClientManager.Connection);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendText(string text, NetworkConnection sender)
    {
		// NameManager github da var
        string h = NameManager.GetPlayerName(sender);
        RecieveText($"{h}={text}");
    }

    [ObserversRpc]
    public void RecieveText(string text)
    {
        chat.text += $"{text}\n";
    }
}
