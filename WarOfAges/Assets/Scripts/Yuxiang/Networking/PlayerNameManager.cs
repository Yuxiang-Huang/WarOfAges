using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerNameManager : MonoBehaviour
{    [SerializeField] TMP_InputField nameInput;

    void Start()
    {
        if (PlayerPrefs.HasKey("username"))
        {
            nameInput.text = PlayerPrefs.GetString("username");
        }
        else
        {
            char[] vowel = new char[] {'a', 'e', 'i', 'o', 'u'};

            nameInput.text = "" + consonant(vowel) + vowel[Random.Range(0, vowel.Length)] + consonant(vowel);

            onNameInputValueChanged();
        }
    }

    public void randomName()
    {
        char[] vowel = new char[] { 'a', 'e', 'i', 'o', 'u' };

        nameInput.text = "" + consonant(vowel) + vowel[Random.Range(0, vowel.Length)] + consonant(vowel);

        onNameInputValueChanged();
    }

    public void onNameInputValueChanged()
    {
        PhotonNetwork.NickName = nameInput.text;
        PlayerPrefs.SetString("username", nameInput.text);
    }

    public static char consonant(char[] vowel)
    {
        char ans;
        do
        {
            ans = (char)('a' + Random.Range(0, 26));
        } while (System.Array.IndexOf(vowel, ans) > -1);

        return ans;
    }
}
