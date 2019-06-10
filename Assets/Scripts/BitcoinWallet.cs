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
	[SerializeField] GameObject BalanceAnimatedLoader; 
	
	[Header("Wallet Receive")]
	[SerializeField] Text AddressText;
	[SerializeField] RawImage AddressQRRawImage;
	[SerializeField] Button AddressQRButton;//TODO click copy address
	[SerializeField] Text CopyAddressButtonText;
	[SerializeField] GameObject QRAnimatedLoader;
	
	
	[Header("Wallet Send")]
	[SerializeField] Button SendButton;//TODO send money
	
	[Header("Private")]
	[SerializeField] InputField MnemonicInputField;
	[SerializeField] Button ImportButton;
	[SerializeField] Button CopyMnemonicButton;
	[SerializeField] Text CopyMnemonicButtonText;
	[SerializeField] InputField PrivateKeyInputField;

	[Header("Logs")]
	[SerializeField] Text LogsText;
	
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
				StartCoroutine(RefreshBalance());
			}
		}
	}

	//Balance JSON example:
	//{
	//	"address": "1DEP8i3QJCsomS4BSMY2RpU1upv62aGvhD",
	//	"total_received": 4433416,
	//	"total_sent": 0,
	//	"balance": 4433416,
	//	"unconfirmed_balance": 0,
	//	"final_balance": 4433416,
	//	"n_tx": 7,
	//	"unconfirmed_n_tx": 0,
	//	"final_n_tx": 7
	//}
	JSONObject m_balanceData;

	int m_confirmedBalance
	{
		get
		{
			
			int bal = 0;
			if (m_balanceData != null && !m_balanceData.IsNull && m_balanceData.GetField(ref bal, "balance"))
			{
				return bal;
			}
			else
			{
				Log("Returning invalid m_confirmedBalance", true);
			}
			return int.MinValue;
		}
	}

	int m_unconfirmedBalance
	{
		get
		{
			int bal = 0;
			if (m_balanceData != null && !m_balanceData.IsNull && m_balanceData.GetField(ref bal, "unconfirmed_balance"))
			{
				return bal;
			}
			else
			{
				Log("Returning invalid m_unconfirmedBalance", true);
			}
			return int.MinValue;
		}
	}
	
	string m_address
	{
		get
		{
			return WalletMnemonic.DeriveExtKey().GetPublicKey().GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString();
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
		BalanceRefreshButton.onClick.AddListener(() =>
		{
			StartCoroutine(RefreshBalance());
		});
		CopyMnemonicButton.onClick.AddListener(() =>
		{
			CopyTextToClipboard(MnemonicInputField.text);
			StartCoroutine(Animatetext("Copied :D", CopyMnemonicButtonText));
		} );
		AddressQRButton.onClick.AddListener(() =>
		{
			CopyTextToClipboard(m_address);
			StartCoroutine(Animatetext("Address copied :D", AddressText));
		});
	}

	void Start()
	{
		//position rough review helmet urban great custom carpet custom honey mango talent
		GenerateWallet();
	}

	void RefreshWalletUI()
	{
		//INTERESTING every mnemonic can be derived to its equivalent private key
		//Which is used to sign transactions
		MnemonicInputField.text = WalletMnemonic.ToString();
		PrivateKeyInputField.text = WalletMnemonic.DeriveExtKey().ToString(Network.Main);
		AddressText.text = m_address;
		StopAllCoroutines();
		
		//INTERESTING Mobile wallets uses "bitcoin:<address>" as the encoded string in QR images
		StartCoroutine(LoadQR("bitcoin:"+m_address));
	}

	void Log(string newText, bool error = false)
	{
		if (error)
			Debug.LogError(newText);
		else
			Debug.Log(newText);

		LogsText.text += "\n> " + (error ? "<color=#ff0000ff><b>" : "") + newText + (error ? "</b></color>" : "");
	}
	
	void GenerateWallet(string mnemonicStr = "")
	{
		if (mnemonicStr != "")
		{
			//INTERESTING mnemonic can be imported
			Log("Importing wallet from mnemonic "+mnemonicStr.Substring(0,10)+"...");
			WalletMnemonic = new Mnemonic(mnemonicStr);
		}
		else
		{
			Log("Generating new wallet");
			//INTERESTING mnemonic can be randomly generated totally offline
			WalletMnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
		}
	}


	IEnumerator RefreshBalance()
	{
		BalanceAnimatedLoader.SetActive(true);
		BalanceRefreshButton.gameObject.SetActive(false);
		BalanceAmountText.text = "Loading...";
		yield return new WaitForSeconds(0.3f);
		
		//INTERESTING as the blockchain is public anybody can check the balance for any address
		//In this case we are trusting an external company but this url request could be replaced by your own bitcoin node
		WWW www = new WWW("https://api.blockcypher.com/v1/btc/main/addrs/"+m_address+"/balance");
		// Documentation @ https://www.blockcypher.com/dev/bitcoin/#address-balance-endpoint
		
		yield return www;
		if (www.error != null)
		{
			Log("Cannot get balance from \""+"\"\nError: "+www.error);
			BalanceAmountText.text = "Couldn't refresh balance :C";
		}
		else
		{
			m_balanceData = new JSONObject(www.text);
			Log("New balance data: "+m_balanceData.ToString(true));
			BalanceAmountText.text = "Confirmed: "+m_confirmedBalance+"[sat]";
			if (m_unconfirmedBalance != 0)
				BalanceAmountText.text += "\t\tUnconfirmed: " + m_unconfirmedBalance;
		}
		BalanceAnimatedLoader.SetActive(false);
		BalanceRefreshButton.gameObject.SetActive(true);
	}
	
	
	// Utilities
	void CopyTextToClipboard(string txt)
	{
		TextEditor te = new TextEditor();
		te.text = txt;
		te.SelectAll();
		te.Copy();
	}
	IEnumerator LoadQR(string str)
	{
		QRAnimatedLoader.SetActive(true);
		AddressQRButton.gameObject.SetActive(false);
		AddressQRRawImage.texture = null;
		yield return new WaitForSeconds(0.3f);
		WWW www = new WWW("http://chart.apis.google.com/chart?cht=qr&chs=300x300&chl="+str);
		yield return www;
		if (www.error != null)
		{
			Log("Cannot get QR code from \"" + www.url + "\"\nError: " + www.error, true);
			QRAnimatedLoader.SetActive(false);
			AddressQRButton.gameObject.SetActive(false);
		}
		else
		{
			AddressQRRawImage.texture = www.texture;
			QRAnimatedLoader.SetActive(false);
			AddressQRButton.gameObject.SetActive(true);
		}
	}

	IEnumerator Animatetext(string newAnimatedText, Text uiText)
	{
		string lastText = uiText.text;
		for (int i = 0; i <= newAnimatedText.Length; i++)
		{
			uiText.text = newAnimatedText.Substring(0, i);
			yield return new WaitForSecondsRealtime(.03f);
		}
		yield return new WaitForSecondsRealtime(2.0f);
		uiText.text = lastText;
	}
}
