using UnityEngine;
using GoogleMobileAds.Api;
using TMPro;
using UnityEngine.UI;


public class AdMobAdsScript : MonoBehaviour
{
    public string appId = "ca-app-pub-2031850231197911~8065656844";


    //android ids
    string bannerId = "ca-app-pub-2031850231197911/1867606663";
    string interId = "ca-app-pub-2031850231197911/1915181466";
    string rewardId = "ca-app-pub-2031850231197911/8049871634";

    BannerView bannerView;
    InterstitialAd interstitialAd;
    RewardedAd rewardedAd;
    
    // Variable pour suivre si une récompense a été obtenue
    private bool hasEarnedReward = false;

    private void Start()
    {
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        
        Debug.Log("Initialisation de AdMob avec l'app ID: " + appId);
        
        // Vérifier que l'ID n'est pas vide
        if (string.IsNullOrEmpty(appId))
        {
            Debug.LogError("App ID est vide - initialisation de AdMob impossible");
            return;
        }
        
        MobileAds.Initialize(initStatus => {
            Debug.Log("AdMob initialized avec statut: " + initStatus);
            
            // Vérifier chaque adaptateur (réseaux publicitaires)
            var adapterStatusMap = initStatus.getAdapterStatusMap();
            foreach (var adapterStatus in adapterStatusMap)
            {
                string adapterName = adapterStatus.Key;
                var status = adapterStatus.Value;
                Debug.Log($"Adaptateur {adapterName}: {status.InitializationState}, {status.Description}");
            }
            
            // Précharger les pubs après l'initialisation
            LoadBannerAd();
            LoadRewardedAd();
            LoadInterstitialAd();
        });
    }

  #region banner

    public void LoadBannerAd() {
        //create a banner
        CreateBannerView();
        //listen to banner events
        ListenToBannerEvents();
        //load the banner
        if (bannerView==null) {
            CreateBannerView();
        }
        var adRequest = new AdRequest();
        adRequest.Keywords.Add("unity-admob-sample");

        print("Loading banner ad...");
        bannerView.LoadAd(adRequest); // show the banner ads on the screen
    }
    void CreateBannerView() {

        if (bannerView!=null) {
            bannerView.Destroy();
        }
        bannerView = new BannerView(bannerId, AdSize.Banner, AdPosition.Bottom);
    }

    void ListenToBannerEvents() 
    {
        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + bannerView.GetResponseInfo());
        };
        // Raised when an ad fails to load into the banner view.
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner view failed to load an ad with error : "
                + error);
        };
        // Raised when the ad is estimated to have earned money.
        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log("Banner view paid {0} {1}."+
                adValue.Value+
                adValue.CurrencyCode);
        };
        // Raised when an impression is recorded for an ad.
        bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner view full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner view full screen content closed.");
        };
    }

    public void DestroyBannerAd() {

        if (bannerView!=null) {
            print("Destroying banner ad...");
            bannerView.Destroy();
            bannerView = null;
        }
    }

  #endregion

  #region interstitial

    public void LoadInterstitialAd() {

        if (interstitialAd!=null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }
        var adRequest = new AdRequest();
        adRequest.Keywords.Add("unity-admob-sample");

        Debug.Log("Tentative de chargement de la pub interstitielle avec ID: " + interId);

        InterstitialAd.Load(interId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
              if (error!=null||ad==null)
              {
                Debug.LogError("Interstitial ad failed to load: " + (error != null ? error.ToString() : "ad is null"));
                return;
              }

            Debug.Log("Interstitial ad loaded successfully! " + ad.GetResponseInfo());

            interstitialAd = ad;
            InterstitialEvent(interstitialAd);
        });

    }
    public void ShowInterstitialAd() {
        try 
        {
            Debug.Log("Tentative d'affichage de la pub interstitielle");
            
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                Debug.Log("Affichage de la pub interstitielle");
                interstitialAd.Show();
            }
            else {
                Debug.LogError("Interstitial ad not ready! Tentative de rechargement");
                LoadInterstitialAd();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception lors de l'affichage de la pub interstitielle: " + e.Message);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception lors de l'affichage de la pub interstitielle: " + e.Message);
        }
    }
    public void InterstitialEvent(InterstitialAd ad) {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) => 
        {
            Debug.Log("Interstitial ad paid " + adValue.Value + " " + adValue.CurrencyCode);
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed.");
            // Recharger une nouvelle pub pour la prochaine fois
            LoadInterstitialAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content with error: " + error.ToString());
            // Tenter de recharger la pub
            LoadInterstitialAd();
        };
    }

  #endregion


    #region rewarded
 public void LoadRewardedAd() {

        if (rewardedAd!=null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
        var adRequest = new AdRequest();
        adRequest.Keywords.Add("unity-admob-sample");

        Debug.Log("Tentative de chargement de la pub récompensée avec ID: " + rewardId);

        RewardedAd.Load(rewardId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Rewarded ad failed to load: " + (error != null ? error.ToString() : "ad is null"));
                return;
            }

            Debug.Log("Rewarded ad loaded successfully!");
            rewardedAd = ad;
            RegisterRewardedAdEvents();
        });
    }
    public void ShowRewardedAd() {
        try
        {
            // Réinitialiser l'état de la récompense
            hasEarnedReward = false;
            
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                Debug.Log("Tentative d'affichage de la pub récompensée");
                rewardedAd.Show((Reward reward) =>
                {
                    Debug.Log($"Rewarded ad reward received: {reward.Amount} {reward.Type}");
                    
                    // Marquer simplement que la récompense a été obtenue
                    // Ne pas appeler ContinueGame ici, on le fera à la fermeture de la pub
                    hasEarnedReward = true;
                });
            }
            else {
                Debug.LogError("Rewarded ad not ready, reloading...");
                LoadRewardedAd(); // Tenter de recharger la pub
                
                // Informer UIManager que la pub n'est pas disponible
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OnRewardedAdClosed(true);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception lors de l'affichage de la pub récompensée: " + e.Message);
            // Informer UIManager de l'erreur
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnRewardedAdClosed(true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception lors de l'affichage de la pub récompensée: " + e.Message);
            // Informer UIManager de l'erreur
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnRewardedAdClosed(true);
            }
        }
    }
    private void RegisterRewardedAdEvents()
    {
        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad closed");
            
            // Si une récompense a été obtenue, appeler ContinueGame
            if (hasEarnedReward)
            {
                Debug.Log("La récompense a été obtenue, poursuite du jeu...");
                
                try
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ContinueGame(); 
                    }
                    else
                    {
                        Debug.LogError("GameManager.Instance est null dans OnAdFullScreenContentClosed");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Exception lors de l'appel à ContinueGame: " + e.Message);
                }
                
                // Réinitialiser après utilisation
                hasEarnedReward = false;
            }
            else
            {
                Debug.Log("La pub a été fermée sans récompense");
                
                // Vérifier si la récompense a été accordée
                // Si ce n'est pas le cas, UIManager doit être informé pour réactiver le bouton
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OnRewardedAdClosed(false);
                }
            }
            
            // Recharger une nouvelle pub
            LoadRewardedAd();
        };

        rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to show: " + error.ToString());
            LoadRewardedAd(); // Tenter de recharger en cas d'échec
            
            // Informer UIManager de l'échec pour qu'il puisse réactiver le bouton
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnRewardedAdClosed(true);
            }
        };

        rewardedAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad opened");
        };

        rewardedAd.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"Rewarded ad paid {adValue.Value} {adValue.CurrencyCode}");
        };
    }
    #endregion

  #region extra

  #endregion
        
    // Méthode pour vérifier si la pub interstitielle est chargée
    public bool IsInterstitialAdLoaded()
    {
        return interstitialAd != null && interstitialAd.CanShowAd();
    }
    
    // Nouvelle méthode pour vérifier si la pub récompensée est chargée
    public bool IsRewardedAdLoaded()
    {
        return rewardedAd != null && rewardedAd.CanShowAd();
    }
}
