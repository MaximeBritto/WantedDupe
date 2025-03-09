using UnityEngine;
using GoogleMobileAds.Api;
using TMPro;
using UnityEngine.UI;


public class AdMobAdsScript : MonoBehaviour
{
    public string appId = "";


    //android ids
    string bannerId = "ca-app-pub-3940256099942544/6300978111";
    string interId = "ca-app-pub-3940256099942544/1033173712";
    string rewardId = "ca-app-pub-3940256099942544/5224354917";
    string nativeId = "ca-app-pub-3940256099942544/2247696110";

    BannerView bannerView;
    InterstitialAd interstitialAd;
    RewardedAd rewardedAd;

    private void Start()
    {
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize(initStatus => {

            print("AdMob initialized");

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


  #region extra

  #endregion
        
}
