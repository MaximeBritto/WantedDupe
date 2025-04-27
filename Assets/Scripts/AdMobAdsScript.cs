using UnityEngine;
using GoogleMobileAds.Api;
using TMPro;
using UnityEngine.UI;
using System.Collections;


public class AdMobAdsScript : MonoBehaviour
{
    
    //test appId
    public string appId = "ca-app-2031850231197911~8065656844";

    //test android ids
    string bannerId = "ca-app-pub-2031850231197911/1867606663";
    string interId = "ca-app-pub-2031850231197911/1915181466";
    string rewardId = "ca-app-pub-2031850231197911/8049871634";
    
    /*
    //build appId
    public string appId = "ca-app-pub-7927443612072802~1904766119";


    //build android ids
    string bannerId = "ca-app-pub-7927443612072802/9591684442";
    string interId = "ca-app-pub-7927443612072802/8278602770";
    string rewardId = "ca-app-pub-7927443612072802/5660178297";
    */

    BannerView bannerView;
    InterstitialAd interstitialAd;
    RewardedAd rewardedAd;
    
    // Variable pour suivre si une récompense a été obtenue
    private bool hasEarnedReward = false;

    private void Start()
    {
        Debug.Log("[AdMob] Initialisation AdMob avec appId: " + appId);
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize(initStatus => {

            Debug.Log("[AdMob] AdMob initialized");
            
            // Log les états d'initialisation
            foreach (var adapterStatus in initStatus.getAdapterStatusMap())
            {
                Debug.Log($"[AdMob] Adapter {adapterStatus.Key}: {adapterStatus.Value.InitializationState}, Description: {adapterStatus.Value.Description}");
            }
            
            // Précharger des publicités après initialisation
            LoadBannerAd();
            LoadInterstitialAd();
            LoadRewardedAd();
        });
    }

  #region banner

    public void LoadBannerAd() {
        Debug.Log("[AdMob] Chargement de la bannière publicitaire avec ID: " + bannerId);
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

        Debug.Log("[AdMob] Loading banner ad...");
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
            Debug.Log("[AdMob] Banner view loaded an ad with response : "
                + bannerView.GetResponseInfo());
        };
        // Raised when an ad fails to load into the banner view.
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("[AdMob] Banner view failed to load an ad with error : "
                + error);
        };
        // Raised when the ad is estimated to have earned money.
        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log("[AdMob] Banner view paid {0} {1}."+
                adValue.Value+
                adValue.CurrencyCode);
        };
        // Raised when an impression is recorded for an ad.
        bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdMob] Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        bannerView.OnAdClicked += () =>
        {
            Debug.Log("[AdMob] Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdMob] Banner view full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdMob] Banner view full screen content closed.");
        };
    }

    public void DestroyBannerAd() {

        if (bannerView!=null) {
            Debug.Log("[AdMob] Destroying banner ad...");
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

        Debug.Log($"[AdMob] Chargement de l'interstitiel avec ID: {interId}");
        InterstitialAd.Load(interId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
              if (error!=null||ad==null)
              {
                Debug.LogError($"[AdMob] Interstitial ad failed to load: {error?.GetMessage()}");
                return;
              }

            Debug.Log("[AdMob] Interstitial ad loaded successfully: " + ad.GetResponseInfo());

            interstitialAd = ad;
            InterstitialEvent(interstitialAd);
        });

    }
    public void ShowInterstitialAd() {
        try 
        {
            Debug.Log("[AdMob] Tentative d'affichage de la pub interstitielle");
            
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                Debug.Log("[AdMob] Affichage de la pub interstitielle");
                interstitialAd.Show();
            }
            else {
                Debug.LogError("[AdMob] Interstitial ad not ready! Tentative de rechargement");
                LoadInterstitialAd();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AdMob] Exception lors de l'affichage de la pub interstitielle: " + e.Message);
        }
    }
    public void InterstitialEvent(InterstitialAd ad) {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) => 
        {
            Debug.Log("[AdMob] Interstitial ad paid " + adValue.Value + " " + adValue.CurrencyCode);
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdMob] Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("[AdMob] Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdMob] Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdMob] Interstitial ad full screen content closed.");
            // Recharger une nouvelle pub pour la prochaine fois
            LoadInterstitialAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("[AdMob] Interstitial ad failed to open full screen content with error: " + error.ToString());
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

        Debug.Log($"[AdMob] Chargement de la pub récompensée avec ID: {rewardId}");
        RewardedAd.Load(rewardId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMob] Rewarded ad failed to load: {error?.GetMessage()}");
                return;
            }

            Debug.Log("[AdMob] Rewarded ad loaded successfully: " + ad.GetResponseInfo());
            rewardedAd = ad;
            RegisterRewardedAdEvents();
        });
    }
    public void ShowRewardedAd() {
        try
        {
            // Réinitialiser l'état de la récompense
            hasEarnedReward = false;
            
            Debug.Log("[AdMob] ShowRewardedAd - rewardedAd est " + (rewardedAd != null ? "non null" : "null"));
            if (rewardedAd != null)
            {
                Debug.Log("[AdMob] ShowRewardedAd - CanShowAd: " + rewardedAd.CanShowAd());
            }
            
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                Debug.Log("[AdMob] Tentative d'affichage de la pub récompensée");
                rewardedAd.Show((Reward reward) =>
                {
                    Debug.Log($"[AdMob] Rewarded ad reward received: {reward.Amount} {reward.Type}");
                    
                    // Marquer simplement que la récompense a été obtenue
                    // Ne pas appeler ContinueGame ici, on le fera à la fermeture de la pub
                    hasEarnedReward = true;
                });
            }
            else {
                Debug.LogError("[AdMob] Rewarded ad not ready, reloading...");
                LoadRewardedAd(); // Tenter de recharger la pub
                
                // Informer UIManager que la pub n'est pas disponible
                if (UIManager.Instance != null)
                {
                    Debug.Log("[AdMob] Notifying UIManager about ad error");
                    UIManager.Instance.OnRewardedAdClosed(true);
                }
                else
                {
                    Debug.LogError("[AdMob] UIManager.Instance est null dans ShowRewardedAd");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AdMob] Exception lors de l'affichage de la pub récompensée: " + e.Message);
            // Informer UIManager de l'erreur
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnRewardedAdClosed(true);
            }
            else
            {
                Debug.LogError("[AdMob] UIManager.Instance est null dans l'exception de ShowRewardedAd");
            }
        }
    }

    private void RegisterRewardedAdEvents()
    {
        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdMob] Rewarded ad closed");
            
            // Si une récompense a été obtenue, appeler ContinueGame
            if (hasEarnedReward)
            {
                Debug.Log("[AdMob] La récompense a été obtenue, poursuite du jeu...");
                
                try
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ContinueGame(); 
                    }
                    else
                    {
                        Debug.LogError("[AdMob] GameManager.Instance est null dans OnAdFullScreenContentClosed");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[AdMob] Exception lors de l'appel à ContinueGame: " + e.Message);
                }
                
                // Réinitialiser après utilisation
                hasEarnedReward = false;
            }
            else
            {
                Debug.Log("[AdMob] La pub a été fermée sans récompense");
                
                // Vérifier si la récompense a été accordée
                // Si ce n'est pas le cas, UIManager doit être informé pour réactiver le bouton
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OnRewardedAdClosed(false);
                }
                else
                {
                    Debug.LogError("[AdMob] UIManager.Instance est null dans OnAdFullScreenContentClosed (sans récompense)");
                }
            }
            
            // Recharger une nouvelle pub
            LoadRewardedAd();
        };

        rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[AdMob] Échec d'affichage de la publicité récompensée: {error.GetMessage()}");
            
            // Informer UIManager de l'erreur
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnRewardedAdClosed(true);
            }
            else
            {
                Debug.LogError("[AdMob] UIManager.Instance est null dans OnAdFullScreenContentFailed");
            }
            
            // Tenter de recharger immédiatement
            LoadRewardedAd();
        };

        rewardedAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdMob] Publicité récompensée ouverte avec succès");
        };

        rewardedAd.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"[AdMob] Publicité récompensée payée: {adValue.Value} {adValue.CurrencyCode}");
        };
    }
    #endregion

  #region extra

    public bool IsInterstitialReady() {
        Debug.Log("[AdMob] IsInterstitialReady: " + (interstitialAd != null && interstitialAd.CanShowAd()));
        return interstitialAd != null && interstitialAd.CanShowAd();
    }

    public bool IsRewardedAdReady() {
        bool isReady = rewardedAd != null && rewardedAd.CanShowAd();
        Debug.Log("[AdMob] IsRewardedAdReady: " + isReady);
        return isReady;
    }

  #endregion
        
    // Méthode pour vérifier si la pub interstitielle est chargée
    public bool IsInterstitialAdLoaded()
    {
        bool isLoaded = interstitialAd != null && interstitialAd.CanShowAd();
        Debug.Log("[AdMob] IsInterstitialAdLoaded: " + isLoaded);
        return isLoaded;
    }
    
    // Nouvelle méthode pour vérifier si la pub récompensée est chargée
    public bool IsRewardedAdLoaded()
    {
        bool isLoaded = rewardedAd != null && rewardedAd.CanShowAd();
        Debug.Log("[AdMob] IsRewardedAdLoaded: " + isLoaded);
        return isLoaded;
    }
}

