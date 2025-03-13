using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelCondition : MonoBehaviour
{
	public event Action<bool> ConditionCompleteEvent; // Sửa thành Action<bool> để truyền trạng thái thắng/thua

	protected Text m_txt;

    protected bool m_conditionCompleted = false;

    public virtual void Setup(float value, Text txt)
    {
        m_txt = txt;
    }

    public virtual void Setup(float value, Text txt, GameManager mngr)
    {
        m_txt = txt;
    }

    public virtual void Setup(float value, Text txt, BoardController board)
    {
        m_txt = txt;
    }

    protected virtual void UpdateText() { }

	protected virtual void OnConditionComplete(bool isWin)
	{
		if (ConditionCompleteEvent != null)
		{
			ConditionCompleteEvent(isWin);
		}
	}

	protected virtual void OnDestroy()
    {

    }
}
