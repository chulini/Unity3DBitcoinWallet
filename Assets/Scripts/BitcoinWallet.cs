﻿using System.Collections;
using System.Collections.Generic;
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
	[SerializeField] GameObject QRAnimatedLoader;
	
	[Header("Wallet Send")]
	[SerializeField] Button SendButton;
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
	[SerializeField] Button RevealPrivateButton;

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

	int m_balance
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
	
	BitcoinAddress m_address
	{
		get
		{
			return WalletMnemonic.DeriveExtKey().GetPublicKey().GetAddress(ScriptPubKeyType.Legacy, Network.Main);
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
			RevealPrivateButton.gameObject.SetActive(true);
		});
		BalanceRefreshButton.onClick.AddListener(() =>
		{
			StartCoroutine(RefreshBalance());
		});
		CopyMnemonicButton.onClick.AddListener(() =>
		{
			CopyTextToClipboard(MnemonicInputField.text);
			StartCoroutine(Animatetext("Copied :D", CopyMnemonicButtonText));
			RevealPrivateButton.gameObject.SetActive(true);
		} );
		AddressQRButton.onClick.AddListener(() =>
		{
			CopyTextToClipboard(m_address.ToString());
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
		RevealPrivateButton.onClick.AddListener(() =>
		{
			RevealPrivateButton.gameObject.SetActive(false);
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
		Log("Requesting transaction outputs for address "+m_address.ToString());
		SetSendPanelInteractable(false);
		
		//INPUT: money coming from an address
		//OUTPUT: money going to an address
		
		//Request all unspent transactions related to this address
		//https://live.blockcypher.com/btc/address/<address> shows the same but nicely
		
		WWW wwwTxo = new WWW("https://blockchain.info/unspent?active=" + m_address.ToString() + "");
		yield return wwwTxo;
		
		if (wwwTxo.error != null)
		{
			Log("Cannot get utxo from \""+wwwTxo.url+"\"\nError: "+wwwTxo.error);
			SetSendPanelInteractable(true);
		}
		else
		{
//			{
//				"unspent_outputs": [
	//				{
	//					"tx_hash": "aed0a61f789e80975425685ef65ba0a0e388db0c001dfd9e548e896fe39c1067",
	//					"tx_hash_big_endian": "67109ce36f898e549efd1d000cdb88e3a0a05bf65e68255497809e781fa6d0ae",
	//					"tx_output_n": 0,
	//					"script": "76a914717ee27eca5ed0bd30fde351a19439ed96bdcb3f88ac",
	//					"value": 2176,
	//					"value_hex": "0880",
	//					"confirmations": 0,
	//					"tx_index": 461965676
	//				}
//				]
//			}
			JSONObject utxos = new JSONObject(wwwTxo.text);
			//Get all unspent outputs using my address
			List<Coin> unspentCoins = new List<Coin>();
			for (int i = 0; i < utxos["unspent_outputs"].Count; i++)
			{
				string txHash = utxos["unspent_outputs"][i]["tx_hash_big_endian"].str;
				uint outputIndex = (uint) utxos["unspent_outputs"][i]["tx_output_n"].n;
				int valueUnspent = (int) utxos["unspent_outputs"][i]["value"].n;
				Log(valueUnspent + " unspent!");
				
				unspentCoins.Add(new Coin(
					new uint256(txHash), //Hash of the transaction this output is comming from 
					outputIndex, //Index of the transaction output within the transaction
					Money.Satoshis(valueUnspent), //Amount of money this output has
					m_address.ScriptPubKey)); //Script to solve to spend this output
			}
			
			//+info about how to use TransactionBuilder can be found here
			//https://csharp.hotexamples.com/examples/NBitcoin/TransactionBuilder/-/php-transactionbuilder-class-examples.html
			TransactionBuilder txBuilder = Network.Main.CreateTransactionBuilder();
			txBuilder.AddKeys(WalletMnemonic.DeriveExtKey().PrivateKey);
			txBuilder.AddCoins(unspentCoins);
			txBuilder.Send(BitcoinAddress.Create(recipientAddress, Network.Main), Money.Satoshis(amountSat));
			txBuilder.SendFees(Money.Satoshis(feeSat));
			txBuilder.SetChange(m_address);
			Transaction tx = txBuilder.BuildTransaction(true);
			
			Log(tx.ToHex().ToString());
			
			
			//Submit raw transaction to a bitcoin node
			//In this case we use blockchain.info API
			WWWForm form = new WWWForm();
			form.AddField("tx",tx.ToHex());
			WWW wwwRawTx = new WWW("https://blockchain.info/pushtx", form);
			yield return wwwRawTx;

			if (wwwRawTx.error != null)
			{
				Log("Error submitting raw transaction " + wwwRawTx.error, true);
			}
			else
			{
				Log(wwwTxo.text);
				Log("Transaction sent!");
				yield return new WaitForSeconds(3f);
				Application.OpenURL("https://www.blockchain.com/btc/address/"+m_address);
				yield return StartCoroutine(RefreshBalance());
			}

		}
		
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
		//inch motion chef age special plastic right food vibrant ask police push
		GenerateWallet();
		UpdateTotalToSpend();
	}

	void RefreshWalletUI()
	{
		//INTERESTING every mnemonic can be derived to its equivalent private key
		//Which is used to sign transactions
		MnemonicInputField.text = WalletMnemonic.ToString();
		PrivateKeyInputField.text = WalletMnemonic.DeriveExtKey().ToString(Network.Main);
		AddressText.text = m_address.ToString();
		StopAllCoroutines();
		
		//INTERESTING Mobile wallets uses "bitcoin:<address>" as the encoded string in QR images
		StartCoroutine(LoadQR("bitcoin:"+m_address.ToString()));
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
		WWW www = new WWW("https://api.blockcypher.com/v1/btc/main/addrs/"+m_address.ToString()+"/balance");
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
			BalanceAmountText.text = "Confirmed: "+m_balance+"[sat]";
			if (m_unconfirmedBalance != 0)
				BalanceAmountText.text += "\t(Waiting: " + m_unconfirmedBalance+"[sat])\tFinal:"+(m_balance+m_unconfirmedBalance)+"[sat]";
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
