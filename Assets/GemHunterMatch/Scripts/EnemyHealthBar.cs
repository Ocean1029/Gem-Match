using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>
    /// Health bar component that displays enemy health as a percentage.
    /// Uses Unity UI (Canvas + Slider) in World Space mode.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas m_Canvas;
        [SerializeField] private Slider m_HealthSlider;
        [SerializeField] private Image m_FillImage;
        
        [Header("Visual Settings")]
        [SerializeField] private Vector3 m_Offset = new Vector3(0, 0.6f, 0); // Offset above enemy
        [SerializeField] private Color m_HealthBarColor = new Color(1f, 0f, 0f, 1f); // Red - Health bar color
        [SerializeField] private bool m_AlwaysShow = true; // Always show or only when damaged
        
        private Transform m_EnemyTransform;
        private int m_MaxHealth;
        private int m_CurrentHealth;
        
        private void Awake()
        {
            // Ensure canvas is set to World Space with correct settings
            if (m_Canvas != null)
            {
                m_Canvas.renderMode = RenderMode.WorldSpace;
                
                // Set canvas size to be small (in world units)
                RectTransform canvasRect = m_Canvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    canvasRect.sizeDelta = new Vector2(100, 20); // Small size in canvas units
                    canvasRect.localScale = Vector3.one * 0.01f; // Scale down to world space
                }
            }
            
            // Set slider size
            if (m_HealthSlider != null)
            {
                RectTransform sliderRect = m_HealthSlider.GetComponent<RectTransform>();
                if (sliderRect != null)
                {
                    sliderRect.anchorMin = new Vector2(0, 0);
                    sliderRect.anchorMax = new Vector2(1, 1);
                    sliderRect.offsetMin = Vector2.zero;
                    sliderRect.offsetMax = Vector2.zero;
                }
            }
            
            // Set initial color (always red)
            if (m_FillImage != null)
            {
                m_FillImage.color = m_HealthBarColor;
                Debug.Log($"[EnemyHealthBar] Awake - Setting color to {m_HealthBarColor}, FillImage={m_FillImage.name}");
            }
            else
            {
                Debug.LogWarning("[EnemyHealthBar] Awake - Fill Image is NULL! Cannot set color.");
            }
        }
        
        private void LateUpdate()
        {
            // Follow enemy position
            if (m_EnemyTransform != null)
            {
                transform.position = m_EnemyTransform.position + m_Offset;
                
                // Make health bar face the camera
                if (Camera.main != null)
                {
                    transform.rotation = Camera.main.transform.rotation;
                }
            }
        }
        
        /// <summary>
        /// Initialize the health bar with enemy reference and max health
        /// </summary>
        public void Initialize(Transform enemyTransform, int maxHealth)
        {
            m_EnemyTransform = enemyTransform;
            m_MaxHealth = maxHealth;
            m_CurrentHealth = maxHealth;
            
            Debug.Log($"[EnemyHealthBar] Initialize - MaxHealth={maxHealth}, FillImage={m_FillImage}");
            
            UpdateHealthBar();
            
            // Hide if configured to only show when damaged
            if (!m_AlwaysShow)
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Update health bar based on current health value
        /// </summary>
        public void UpdateHealth(int currentHealth)
        {
            m_CurrentHealth = Mathf.Clamp(currentHealth, 0, m_MaxHealth);
            
            UpdateHealthBar();
            
            // Show health bar when damaged if configured
            if (!m_AlwaysShow && m_CurrentHealth < m_MaxHealth)
            {
                gameObject.SetActive(true);
            }
        }
        
        private void UpdateHealthBar()
        {
            if (m_HealthSlider == null)
            {
                Debug.LogWarning("[EnemyHealthBar] UpdateHealthBar - Slider is NULL!");
                return;
            }
            
            float healthPercentage = m_MaxHealth > 0 ? (float)m_CurrentHealth / m_MaxHealth : 0f;
            m_HealthSlider.value = healthPercentage;
            
            // Keep color as red (no color change)
            if (m_FillImage != null)
            {
                m_FillImage.color = m_HealthBarColor;
                Debug.Log($"[EnemyHealthBar] UpdateHealthBar - HP={healthPercentage:P0} ({m_CurrentHealth}/{m_MaxHealth}), Color={m_HealthBarColor}");
            }
            else
            {
                Debug.LogWarning("[EnemyHealthBar] UpdateHealthBar - Fill Image is NULL!");
            }
        }
        
        /// <summary>
        /// Hide the health bar
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}

