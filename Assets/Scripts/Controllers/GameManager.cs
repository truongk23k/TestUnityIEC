using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public bool canReturn;
	public event Action<eStateGame> StateChangedAction = delegate { };

	public enum eLevelMode
	{
		TIMER,
		MOVES,
		AUTOPLAY,
		AUTOLOSE
	}

	public enum eStateGame
	{
		SETUP,
		MAIN_MENU,
		GAME_STARTED,
		PAUSE,
		GAME_OVER,
		GAME_WIN
	}

	private eStateGame m_state;
	public eStateGame State
	{
		get { return m_state; }
		private set
		{
			m_state = value;
			StateChangedAction(m_state);
		}
	}


	private GameSettings m_gameSettings;
	private BoardController m_boardController;
	private UIMainManager m_uiMenu;
	private LevelCondition m_levelCondition;

	private void Awake()
	{
		State = eStateGame.SETUP;
		m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
		m_uiMenu = FindObjectOfType<UIMainManager>();
		if (m_uiMenu != null)
		{
			m_uiMenu.Setup(this);
		}
		else
		{
			Debug.LogError("UIMainManager not found in scene!");
		}
	}

	void Start()
	{
		State = eStateGame.MAIN_MENU;
	}

	void Update()
	{
		if (m_boardController != null) m_boardController.Update();
	}

	internal void SetState(eStateGame state)
	{
		State = state;

		if (State == eStateGame.PAUSE)
		{
			DOTween.PauseAll();
		}
		else
		{
			DOTween.PlayAll();
		}
	}

	public void LoadLevel(eLevelMode mode)
	{
		// Xóa level cũ nếu có
		ClearLevel();

		// Khởi tạo BoardController mới
		m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
		m_boardController.StartGame(this, m_gameSettings);

		// Thiết lập LevelCondition dựa trên mode
		if (mode == eLevelMode.MOVES || mode == eLevelMode.AUTOPLAY || mode == eLevelMode.AUTOLOSE)
		{
			canReturn = false;
			m_levelCondition = this.gameObject.AddComponent<LevelMoves>();
			m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), m_boardController);
		}
		else if (mode == eLevelMode.TIMER)
		{
			canReturn = true;
			m_levelCondition = this.gameObject.AddComponent<LevelTime>();
			m_levelCondition.Setup(m_gameSettings.LevelTime, m_uiMenu.GetLevelConditionView(), this);
		}

		m_levelCondition.ConditionCompleteEvent += GameOver;
		State = eStateGame.GAME_STARTED;

		// Kích hoạt Autoplay nếu cần
		if (mode == eLevelMode.AUTOPLAY || mode == eLevelMode.AUTOLOSE)
		{
			bool aimToWin = (mode == eLevelMode.AUTOPLAY);
			if (m_boardController != null)
			{
				m_boardController.StartAutoplay(aimToWin);
			}
		}
	}

	public void GameOver(bool isWin = false)
	{
		StartCoroutine(WaitBoardController(isWin));
	}

	internal void ClearLevel()
	{
		if (m_boardController != null)
		{
			m_boardController.Clear();
			Destroy(m_boardController.gameObject);
			m_boardController = null;
		}
	}

	private IEnumerator WaitBoardController(bool isWin)
	{
		while (m_boardController != null && m_boardController.IsBusy)
		{
			yield return new WaitForEndOfFrame();
		}

		yield return new WaitForSeconds(1f);

		if (isWin)
			State = eStateGame.GAME_WIN;
		else
			State = eStateGame.GAME_OVER;

		if (m_levelCondition != null)
		{
			m_levelCondition.ConditionCompleteEvent -= GameOver;
			Destroy(m_levelCondition);
			m_levelCondition = null;
		}
	}
}