using System.Collections;
using UnityEngine;
using NBitcoin;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Network = NBitcoin.Network;

public class BitcoinWallet : MonoBehaviour
{
	
	[Header("Wallet Balance")]
	[SerializeField] Button NewWalletButton;
	[SerializeField] Text BalanceAmountText;//TODO load balance from api
	[SerializeField] Button BalanceRefreshButton;//TODO Same
	
	[Header("Wallet Receive")]
	[SerializeField] Text AddressText;
	[SerializeField] RawImage AddressQRRawImage;
	[SerializeField] Button AddressQRButton;//TODO click copy address
	
	[Header("Wallet Send")]
	[SerializeField] Button SendButton;//TODO send money
	
	[Header("Private")]
	[SerializeField] InputField MnemonicInputField;
	[SerializeField] Button ImportButton;
	[SerializeField] Button CopyMnemonicButton;//TODO copy menmonic onclick
	[SerializeField] InputField PrivateKeyInputField;
	
	Mnemonic m_WalletMnemonic;
	public Mnemonic WalletMnemonic
	{
		get { return m_WalletMnemonic; }
		set
		{
			
			if (m_WalletMnemonic != value)
			{
				m_WalletMnemonic = value;
				RefreshWalletUI();
			}
		}
	}

	void Awake()
	{
		NewWalletButton.onClick.AddListener(() =>
		{
			GenerateWallet();
		});
		
		ImportButton.onClick.AddListener(() =>
		{
			GenerateWallet(MnemonicInputField.text);
		});
	}

	void Start()
	{
		GenerateWallet();
		//position rough review helmet urban great custom carpet custom honey mango talent
	}

	void GenerateWallet(string mnemonicStr = "")
	{	
		WalletMnemonic = (mnemonicStr == "") ? new Mnemonic(Wordlist.English, WordCount.Twelve) : new Mnemonic(mnemonicStr);		
	}
	
	void RefreshWalletUI()
	{
		MnemonicInputField.text = WalletMnemonic.ToString();
		PrivateKeyInputField.text = WalletMnemonic.DeriveExtKey().ToString(Network.Main);
		string address = WalletMnemonic.DeriveExtKey().GetPublicKey().GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString();
		AddressText.text = address;
		
		StopAllCoroutines();
		StartCoroutine(LoadQR("bitcoin:"+address));
	}

	IEnumerator LoadQR(string str)
	{
		AddressQRRawImage.texture = null;
		yield return new WaitForSeconds(0.3f);
		WWW www = new WWW("http://chart.apis.google.com/chart?cht=qr&chs=300x300&chl="+str);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("Cannot get QR code from "+www.url+"\nError:"+www.error);
		}
		else
		{
			AddressQRRawImage.texture = www.texture;
		}
		
	}
}
