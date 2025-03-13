using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
	[SerializeField] private Button btnTimer;

	[SerializeField] private Button btnMoves;

	[SerializeField] private Button btnAutoPlay;
	[SerializeField] private Button btnAutoLose;

	private UIMainManager m_mngr;

	private void Awake()
	{
		btnMoves.onClick.AddListener(OnClickMoves);
		btnAutoPlay.onClick.AddListener(OnClickAutoPlay);
		btnAutoLose.onClick.AddListener(OnClickAutoLose);
		btnTimer.onClick.AddListener(OnClickTimer);
	}

	private void OnDestroy()
	{
		if (btnMoves) btnMoves.onClick.RemoveAllListeners();
		if (btnAutoPlay) btnAutoPlay.onClick.RemoveAllListeners();
		if (btnAutoLose) btnAutoLose.onClick.RemoveAllListeners();
		if (btnTimer) btnTimer.onClick.RemoveAllListeners();
	}

	public void Setup(UIMainManager mngr)
	{
		m_mngr = mngr;
	}

	private void OnClickTimer()
	{
		m_mngr.LoadLevelTimer();
	}

	private void OnClickMoves()
	{
		m_mngr.LoadLevelMoves();
	}
	private void OnClickAutoPlay()
	{
		m_mngr.LoadLevelAutoPlay(); // Gọi chế độ autoplay để thắng
	}

	private void OnClickAutoLose()
	{
		m_mngr.LoadLevelAutoLose(); // Gọi chế độ autoplay để thua
	}

	public void Show()
	{
		this.gameObject.SetActive(true);
	}

	public void Hide()
	{
		this.gameObject.SetActive(false);
	}
}
