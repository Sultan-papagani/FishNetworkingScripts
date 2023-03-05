using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;

public class PlayerNameController : NetworkBehaviour
{
    public TextMeshPro t;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // local player olarak bize ad ver ve diğerlerine duyur
        if (base.IsOwner)
        {
            NameManager.SetName(Random.Range(0,100).ToString());
        }

        // ad listesi güncellenince olanlardan haberdar olalım
        // bunu ayrıca oyuncuların tepesindeki ismi değiştirmek için kullanıyor
        // yani oyunda 10 kişi varsa local olarak onlarada gidiyo
        NameManager.OnNameChange += namechanged;
    }

    public void namechanged(NetworkConnection cc, string x)
    {
        string result = string.Empty;

        // ee tabi obje bizim değilse adı değiştirme
        // o biz değiliz ya

        Debug.Log($"eleman oyuna katıldı: {x}");

        if (cc != base.Owner)
            return;

        if (base.Owner.IsValid)
            // oyuncunun adını listeden getiriyoz (ee x parametresi de ad değilmi ????)
            result = NameManager.GetPlayerName(base.Owner);

        t.text = result;
    }
}
