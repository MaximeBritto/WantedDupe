using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }
    
    [SerializeField] private GameObject particlePrefab;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (particlePrefab == null)
        {
            Debug.LogError("Particle Prefab non assigné dans ParticleManager!");
        }
    }
    
    public void PlayCardEffect(Vector3 position)
    {
        Debug.Log($"Tentative de jouer l'effet de particules à la position: {position}");
        if (particlePrefab == null)
        {
            Debug.LogError("Pas de particle prefab!");
            return;
        }
        
        GameObject particles = Instantiate(particlePrefab, position, Quaternion.identity);
        Debug.Log("Particules créées!");
        Destroy(particles, 2f);
    }
} 