﻿using UnityEngine;
using UnityEngine.UI;

public class LevelTime : LevelCondition
{
	[SerializeField] private float m_time;

	private GameManager m_mngr;

	public override void Setup(float value, Text txt, GameManager mngr)
	{
		base.Setup(value, txt, mngr);

		m_mngr = mngr;

		m_time = value;

		UpdateText();
	}

	private void Update()
	{
		if (m_conditionCompleted) return;

		if (m_mngr.State != GameManager.eStateGame.GAME_STARTED) return;

		m_time -= Time.deltaTime;

		UpdateText();

		if (m_time <= -1f)
		{
			// Sửa: Gọi OnConditionComplete với tham số bool (false vì thua khi hết thời gian)
			OnConditionComplete(false);
		}
	}

	protected override void UpdateText()
	{
		if (m_time < 0f) return;

		m_txt.text = string.Format("TIME:\n{0:00}", m_time);
	}
}
