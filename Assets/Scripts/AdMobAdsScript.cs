using UnityEngine;
using GoogleMobileAds.Api;
using System;

/// <summary>
///  Gestion des publicités Google AdMob (bannière, interstitiel, rewarded) pour Unity.
///  – Supporte les IDs de test ou de production via le booléen useTestIds.
///  – Peut survivre aux changements de scène si besoin (DontDestroyOnLoad).
/// </summary>
public class AdMobAdsScript : MonoBehaviour
{
    /* ───────────────────── PARAMÈTRES ───────────────────── */

#if UNITY_ANDROID
    // IDs OFFICIELS DE TEST FOURNIS PAR GOOGLE (ne génèrent pas de revenus réels)
    private const string BANNER_ID_TEST       = "ca-app-pub-3940256099942544/6300978111";
    private const string INTERSTITIAL_ID_TEST = "ca-app-pub-3940256099942544/1033173712";
    private const string REWARDED_ID_TEST     = "ca-app-pub-3940256099942544/5224354917";

    // <—– REMPLACE ici par TES vrais IDs de production quand tu publies —–>
    private const string BANNER_ID_PROD       = "ca-app-pub-7927443612072802/9591684442";
    private const string INTERSTITIAL_ID_PROD = "ca-app-pub-7927443612072802/8278602770";
    private const string REWARDED_ID_PROD     = "ca-app-pub-7927443612072802/5660178297";
#endif

    [Tooltip("Cochez pour utiliser les IDs de test fournis par Google.")]
    [SerializeField] private bool useTestIds = true;

    /* ───────────────────── PROPRIÉTÉS ───────────────────── */

    private string BannerId       => useTestIds ? BANNER_ID_TEST       : BANNER_ID_PROD;
    private string InterstitialId => useTestIds ? INTERSTITIAL_ID_TEST : INTERSTITIAL_ID_PROD;
    private string RewardedId     => useTestIds ? REWARDED_ID_TEST     : REWARDED_ID_PROD;

    /* ───────────────────── CHAMPS ───────────────────────── */

    private BannerView     bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd     rewardedAd;
    private bool           hasEarnedReward;

    /* ───────────────────── CYCLE DE VIE ─────────────────── */

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // retire-le si tu ne veux pas garder l’objet entre les scènes
    }

    private void Start()
    {
        Debug.Log("[AdMob] Initialisation du SDK…");
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        MobileAds.Initialize(_ =>
        {
            Debug.Log("[AdMob] SDK initialisé");
            LoadBannerAd();
            LoadInterstitialAd();
            LoadRewardedAd();
        });
    }

    /* ───────────────────── BANNIÈRE ─────────────────────── */

    public void LoadBannerAd()
    {
        if (bannerView != null) return; // déjà chargée ?

        bannerView = new BannerView(BannerId, AdSize.Banner, AdPosition.Bottom);
        AttachBannerEvents(bannerView);
        bannerView.LoadAd(new AdRequest());
    }

    private static void AttachBannerEvents(BannerView view)
    {
        view.OnBannerAdLoaded            += () => Debug.Log("[AdMob] Bannière chargée");
        view.OnBannerAdLoadFailed        += e => Debug.LogError("[AdMob] Bannière échec : " + e);
        view.OnAdClicked                 += () => Debug.Log("[AdMob] Bannière cliquée");
        view.OnAdFullScreenContentClosed += () => Debug.Log("[AdMob] Bannière fermé plein écran");
    }

    public void DestroyBannerAd()
    {
        bannerView?.Destroy();
        bannerView = null;
    }

    /* ───────────────────── INTERSTITIEL ─────────────────── */

    public void LoadInterstitialAd()
    {
        interstitialAd?.Destroy();
        interstitialAd = null;

        InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("[AdMob] Interstitiel échec chargement : " + error?.GetMessage());
                return;
            }

            interstitialAd = ad;
            AttachInterstitialEvents(interstitialAd);
        });
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
            interstitialAd.Show();
        else
        {
            Debug.Log("[AdMob] Interstitiel pas prêt – rechargement");
            LoadInterstitialAd();
        }
    }

    private void AttachInterstitialEvents(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () => LoadInterstitialAd();     // enchaîne la suivante
        ad.OnAdFullScreenContentFailed += _ => LoadInterstitialAd();
    }

    /* ───────────────────── REWARDED ─────────────────────── */

    public void LoadRewardedAd()
    {
        rewardedAd?.Destroy();
        rewardedAd = null;

        RewardedAd.Load(RewardedId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("[AdMob] Rewarded échec chargement : " + error?.GetMessage());
                return;
            }

            rewardedAd = ad;
            AttachRewardedEvents(rewardedAd);
        });
    }

    public void ShowRewardedAd()
    {
        hasEarnedReward = false;

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show(reward => hasEarnedReward = true);
        }
        else
        {
            Debug.Log("[AdMob] Rewarded pas prêt – rechargement");
            LoadRewardedAd();
            UIManager.Instance?.OnRewardedAdClosed(true); // signal indisponibilité
        }
    }

    private void AttachRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            if (hasEarnedReward)
                GameManager.Instance?.ContinueGame();
            else
                UIManager.Instance?.OnRewardedAdClosed(false);

            LoadRewardedAd(); // prépare la suivante
        };

        ad.OnAdFullScreenContentFailed += _ =>
        {
            UIManager.Instance?.OnRewardedAdClosed(true);
            LoadRewardedAd();
        };
    }

    /* ───────────────────── HELPERS (accès depuis UIManager) ───────────────────── */

    /// <summary>Retourne true si l’interstitiel est prêt.</summary>
    public bool IsInterstitialReady() => interstitialAd != null && interstitialAd.CanShowAd();

    /// <summary>Alias conservé pour compatibilité avec l’ancien UIManager.</summary>
    public bool IsInterstitialAdLoaded() => IsInterstitialReady();

    /// <summary>Retourne true si la rewarded est prête.</summary>
    public bool IsRewardedReady() => rewardedAd != null && rewardedAd.CanShowAd();

    /// <summary>Alias conservé pour compatibilité avec l’ancien UIManager.</summary>
    public bool IsRewardedAdLoaded() => IsRewardedReady();

    /// <summary>Alias 2ᵉ (UIManager appelle IsRewardedAdReady).</summary>
    public bool IsRewardedAdReady() => IsRewardedReady();
}
