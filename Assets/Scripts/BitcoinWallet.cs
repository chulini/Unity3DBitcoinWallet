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
	[SerializeField] Text BalanceAmountText;
	[SerializeField] Button BalanceRefreshButton;
	[SerializeField] GameObject BalanceAnimatedLoader; 
	
	[Header("Wallet Receive")]
	[SerializeField] Text AddressText;
	[SerializeField] RawImage AddressQRRawImage;
	[SerializeField] Button AddressQRButton;
	[SerializeField] Text CopyAddressButtonText;
	[SerializeField] GameObject QRAnimatedLoader;
	
	[Header("Wallet Send")]
	[SerializeField] Button SendButton;//TODO send money
	[SerializeField] InputField RecipientAddressInputField;
	[SerializeField] InputField SendInputField;
	[SerializeField] InputField FeeInputField;
	[SerializeField] Text TotalSatText;
	
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
		SendButton.onClick.AddListener(() =>
		{
			int sendSat, feeSat;
			if (int.TryParse(SendInputField.text, out sendSat) && int.TryParse(FeeInputField.text, out feeSat))
			{
				StartCoroutine(SendSat(RecipientAddressInputField.text,sendSat, feeSat));
			}
			else
			{
				Log("Amount or Fee can't be parsed as integer", true);
			}
		});
		SendInputField.onValueChanged.AddListener((string txt) =>
		{
			UpdateTotalToSpend();
		});
		FeeInputField.onValueChanged.AddListener((string txt) =>
		{
			UpdateTotalToSpend();
		});
	}

	void UpdateTotalToSpend()
	{
		int sendSat, feeSat;
		if (int.TryParse(SendInputField.text, out sendSat) && int.TryParse(FeeInputField.text, out feeSat))
		{
			TotalSatText.text = (sendSat + feeSat).ToString();
		}
		else
		{
			Log("Amount ("+SendInputField.text+") or Fee ("+FeeInputField.text+") can't be parsed as integer", true);
		}
	}

	IEnumerator SendSat(string recipientAddress, int amountSat, int feeSat)
	{
		Log("Requesting transaction history for address "+m_address);
		SetSendPanelInteractable(false);
		
		//Request transaction history
		WWW www = new WWW("https://api.blockcypher.com/v1/btc/main/addrs/" + m_address + "/full");
		yield return www;
		 /* EXAMPLE OF TRANSACTION HISTORY
{
	"address":"1A1FeiQYswBmxzf3madyfxf1pa9B5vSV6j",
	"total_received":7121,
	"total_sent":0,
	"balance":7121,
	"unconfirmed_balance":597,
	"final_balance":7718,
	"n_tx":1,
	"unconfirmed_n_tx":1,
	"final_n_tx":2,
	"txs":[
		{
			"block_height":-1,
			"block_index":-1,
			"hash":"be2f7ff9ff436cff2e483838c6a80c0403522c7ce57d3daab3bc15790a166c03",
			"addresses":[
				"1CfiJMNr1vUYGqxoT1aHZqg7ikDVFqixe4",
				"1A1FeiQYswBmxzf3madyfxf1pa9B5vSV6j",
				"1GZeJpRCqsG59toSn4iYbQsvtU8y9jzVhM"
			],
			"total":2488487,
			"fees":4520,
			"size":225,
			"preference":"low",
			"relayed_by":"93.147.136.98:8333",
			"received":"2019-06-20T19:39:16.578776151Z",
			"ver":1,
			"double_spend":false,
			"vin_sz":1,
			"vout_sz":2,
			"confirmations":0,
			"inputs":[
				{
					"prev_hash":"0773aa363fe2f72449c96d9644b77803aa7a95705035cdabdc61aa870a82efb5",
					"output_index":1,
					"script":"47304402205ca0f39ddac8ed3eeb0eca0079415aa9e70e99eed95b84dcf2817849c9a5b098022042a9c79034d5caaf3bac676c5e44c0df807c3c9848d9ef281c0f818522529c550121023f62d7d94d2116315cb5bf8cc461c72be706bd4858438f0fc62fe89e884bfe7d",
					"output_value":2493007,
					"sequence":4294967295,
					"addresses":[
						"1CfiJMNr1vUYGqxoT1aHZqg7ikDVFqixe4"
					],
					"script_type":"pay-to-pubkey-hash",
					"age":580162
				}
			],
			"outputs":[
				{
					"value":597,
					"script":"76a91462c55c88857bc97534213b9117da7c35e1bd4a8588ac",
					"addresses":[
						"1A1FeiQYswBmxzf3madyfxf1pa9B5vSV6j"
					],
					"script_type":"pay-to-pubkey-hash"
				},
				{
					"value":2487890,
					"script":"76a914aab6570b3dfc6df45197653671f1134f17cacb4588ac",
					"addresses":[
						"1GZeJpRCqsG59toSn4iYbQsvtU8y9jzVhM"
					],
					"script_type":"pay-to-pubkey-hash"
				}
			]
		},
		{
			"block_hash":"00000000000000000022d20e46e09c74d8876d2dfa18c29e5a44e8a7dd21b422",
			"block_height":580162,
			"block_index":2134,
			"hash":"0773aa363fe2f72449c96d9644b77803aa7a95705035cdabdc61aa870a82efb5",
			"addresses":[
				"1A1FeiQYswBmxzf3madyfxf1pa9B5vSV6j",
				"1CfiJMNr1vUYGqxoT1aHZqg7ikDVFqixe4",
				"1EBftLKFvWL2E41CzGNRz92CnF7Hm2ghgQ"
			],
			"total":2500128,
			"fees":5424,
			"size":225,
			"preference":"low",
			"relayed_by":"13.58.198.168:8333",
			"confirmed":"2019-06-10T21:28:11Z",
			"received":"2019-06-10T20:55:39.385Z",
			"ver":1,
			"double_spend":false,
			"vin_sz":1,
			"vout_sz":2,
			"confirmations":1465,
			"confidence":1,
			"inputs":[
				{
					"prev_hash":"2a7d2d5806fdbfafea8ec4184e935f79ad7cd93f598c3f9091a3bb902e30322b",
					"output_index":1,
					"script":"4730440220591d0fdcdec88d87f4d263b3f6572a6735a908b413135c64e9aa5632bc1695db02200ea8145c8a2e0472e0c01a1e17589509e5c71949cd8e16aae0916bff7268acb9012103f0b076ed8bf8b84237690ca9576a21e8105d3857033fc599b751018deab310ed",
					"output_value":2505552,
					"sequence":4294967295,
					"addresses":[
						"1EBftLKFvWL2E41CzGNRz92CnF7Hm2ghgQ"
					],
					"script_type":"pay-to-pubkey-hash",
					"age":454392
				}
			],
			"outputs":[
				{
					"value":7121,
					"script":"76a91462c55c88857bc97534213b9117da7c35e1bd4a8588ac",
					"addresses":[
						"1A1FeiQYswBmxzf3madyfxf1pa9B5vSV6j"
					],
					"script_type":"pay-to-pubkey-hash"
				},
				{
					"value":2493007,
					"script":"76a9147ffbaa3ba7ce94f0e4d129f5d47958812e2b25b388ac",
					"addresses":[
						"1CfiJMNr1vUYGqxoT1aHZqg7ikDVFqixe4"
					],
					"script_type":"pay-to-pubkey-hash"
				}
			]
		}
	]
}*/
		if (www.error != null)
		{
			Log("Cannot get address transaction history from \""+www.url+"\"\nError: "+www.error);
			SetSendPanelInteractable(true);
		}
		else
		{
			JSONObject transactionHistory = new JSONObject(www.text);
			//Get all unspent outputs
			JSONObject utxos = new JSONObject(JSONObject.Type.ARRAY);
			for (int i = 0; i < transactionHistory["txs"].Count; i++)
			{
				for (int j = 0; j < transactionHistory["txs"][i]["outputs"].Count; j++)
				{
					if (transactionHistory["txs"][i]["outputs"][j]["addresses"][0].ToString() == m_address)
					{
						utxos.Add(transactionHistory["txs"][i]["outputs"][j]);	
					}
				}
			}
			//TODO build transaction here
			
			
		}
		yield return new WaitForSeconds(3f);
		SetSendPanelInteractable(true);
	}

	
	
	void SetSendPanelInteractable(bool on)
	{
		SendInputField.interactable = on;
		SendButton.interactable = on;
		FeeInputField.interactable = on;
		RecipientAddressInputField.interactable = on;
	}

	void Start()
	{
		//mnemonic to remind:
		//position rough review helmet urban great custom carpet custom honey mango talent
		GenerateWallet("position rough review helmet urban great custom carpet custom honey mango talent");
		UpdateTotalToSpend();
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
			//INTERESTING
			//1MHKBnCnJtVwNidC9yq2Hr2dP91BAJx9B5mnemonic can be imported
			Log("Importing wallet from mnemonic "+mnemonicStr.Substring(0,10)+"...");
			WalletMnemonic = new Mnemonic(mnemonicStr);
		}
		else
		{
			Log("Generating new wallet");
			//INTERESTING
			//mnemonic can be randomly generated totally offline
			WalletMnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
		}
	}


	IEnumerator RefreshBalance()
	{
		BalanceAnimatedLoader.SetActive(true);
		BalanceRefreshButton.gameObject.SetActive(false);
		BalanceAmountText.text = "Loading...";
		yield return new WaitForSeconds(0.3f);
		
		//INTERESTING
		//as the blockchain is public anybody can check the balance for any address
		//In this case we are trusting an external company but this url request could be replaced by your own bitcoin node
		WWW www = new WWW("https://api.blockcypher.com/v1/btc/main/addrs/"+m_address+"/balance");
		// Documentation @ https://www.blockcypher.com/dev/bitcoin/#address-balance-endpoint
		
		yield return www;
		if (www.error != null)
		{
			Log("Cannot get balance from \""+www.url+"\"\nError: "+www.error);
			BalanceAmountText.text = "Couldn't refresh balance :C";
		}
		else
		{
			m_balanceData = new JSONObject(www.text);
			Log("New balance data: "+m_balanceData.ToString(true));
			BalanceAmountText.text = "Confirmed: "+m_confirmedBalance+"[sat]";
			if (m_unconfirmedBalance != 0)
				BalanceAmountText.text += "\t\tUnconfirmed: " + m_unconfirmedBalance+"[sat]";
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
