using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace ChildOfEclipse.Coin
{
    /// <summary>
    /// Singleton manager that tracks the player's coin count and updates the UI.
    /// Place this on a persistent GameObject in your scene or on the Player.
    /// </summary>
    public class CoinManager : MonoBehaviour
    {
        #region Singleton

        private static CoinManager _instance;

        /// <summary>
        /// Singleton instance of the CoinManager.
        /// </summary>
        public static CoinManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CoinManager>();

                    if (_instance == null)
                    {
                        Debug.LogWarning("[CoinManager] No instance found in the scene.");
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("UI References")]
        [Tooltip("TMP Text component that displays the current coin count")]
        [SerializeField] private TMP_Text coinCountText;

        [Tooltip("TMP Text component that displays the total coins collected (optional)")]
        [SerializeField] private TMP_Text totalCoinsText;

        [Header("Display Settings")]
        [Tooltip("Format string for the coin count display. {0} = current, {1} = total")]
        [SerializeField] private string displayFormat = "{0}";

        [Tooltip("Format string for the total coins display. {0} = total")]
        [SerializeField] private string totalDisplayFormat = "Total: {0}";

        [Header("Events")]
        [Space]
        [Tooltip("Invoked when coins are collected (passes the amount gained)")]
        public UnityEvent<int> OnCoinsCollected;

        [Tooltip("Invoked when coins are spent (passes the amount spent)")]
        public UnityEvent<int> OnCoinsSpent;

        [Tooltip("Invoked when the coin count changes (passes the new total)")]
        public UnityEvent<int> OnCoinCountChanged;

        #endregion

        #region Private Fields

        private int _currentCoins;
        private int _totalCoinsCollected;

        #endregion

        #region Properties

        /// <summary>
        /// The current number of coins the player has.
        /// </summary>
        public int CurrentCoins => _currentCoins;

        /// <summary>
        /// The total number of coins collected across the entire game session.
        /// </summary>
        public int TotalCoinsCollected => _totalCoinsCollected;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern - ensure only one instance exists
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning(
                    $"[CoinManager] Duplicate instance detected on '{gameObject.name}'. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Start()
        {
            UpdateUI();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add coins to the player's count.
        /// Typically called by the Coin script when a coin is collected.
        /// </summary>
        /// <param name="amount">Number of coins to add</param>
        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CoinManager] AddCoins called with non-positive amount: {amount}", this);
                return;
            }

            _currentCoins += amount;
            _totalCoinsCollected += amount;

            OnCoinsCollected?.Invoke(amount);
            OnCoinCountChanged?.Invoke(_currentCoins);

            UpdateUI();
        }

        /// <summary>
        /// Spend coins if the player has enough. Returns true if successful.
        /// </summary>
        /// <param name="amount">Number of coins to spend</param>
        /// <returns>True if the transaction was successful, false if not enough coins</returns>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CoinManager] SpendCoins called with non-positive amount: {amount}", this);
                return false;
            }

            if (_currentCoins < amount)
            {
                Debug.Log($"[CoinManager] Not enough coins. Have: {_currentCoins}, Need: {amount}");
                return false;
            }

            _currentCoins -= amount;

            OnCoinsSpent?.Invoke(amount);
            OnCoinCountChanged?.Invoke(_currentCoins);

            UpdateUI();
            return true;
        }

        /// <summary>
        /// Set the coin count directly.
        /// </summary>
        /// <param name="amount">New coin count</param>
        public void SetCoins(int amount)
        {
            _currentCoins = Mathf.Max(0, amount);
            OnCoinCountChanged?.Invoke(_currentCoins);
            UpdateUI();
        }

        /// <summary>
        /// Reset the coin count to zero.
        /// </summary>
        public void ResetCoins()
        {
            _currentCoins = 0;
            _totalCoinsCollected = 0;
            OnCoinCountChanged?.Invoke(_currentCoins);
            UpdateUI();
        }

        /// <summary>
        /// Check if the player can afford a given cost.
        /// </summary>
        /// <param name="amount">Cost to check</param>
        /// <returns>True if the player has enough coins</returns>
        public bool CanAfford(int amount)
        {
            return _currentCoins >= amount;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the TMP text elements with the current coin count.
        /// </summary>
        private void UpdateUI()
        {
            if (coinCountText != null)
            {
                coinCountText.text = string.Format(displayFormat, _currentCoins);
            }

            if (totalCoinsText != null)
            {
                totalCoinsText.text = string.Format(totalDisplayFormat, _totalCoinsCollected);
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Live-update the UI when inspector values change
            if (Application.isPlaying)
            {
                UpdateUI();
            }
        }
#endif
    }
}
