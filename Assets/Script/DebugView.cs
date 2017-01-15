using System.Text;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DebugView : MonoBehaviour {

	public static bool m_Debug = true;

	static List<string> m_Error = new List<string>();
	static List<string> m_WriteTxt = new List<string>();
	static List<string> m_SerTxt = new List<string>();
	static string m_FlushLogPath = string.Format("FlushLog_(0).txt", System.DateTime.Now.ToString("yyyyMMddHHmmss"));
	static string m_LogPath = string.Format("Log_(0).txt", System.DateTime.Now.ToString("yyyyMMddHHmmss"));
	static string m_SerMsgPath = string.Format("SerMsg_(0).txt", System.DateTime.Now.ToString("yyyyMMddHHmmss"));
	public static System.DateTime m_StartTime = System.DateTime.Now;

	//刷新計算的時間 幀/秒
	public static float updateInterval = 0.5f;
	
	//最后间隔结束时间
	private static double lastInterval;
	private static int frames = 0;
	private static float currFPS;

	public static bool m_CheckDeviceLost = true;
	public static bool m_IsDeviceLost = false;

	public static void AddView(string sLog)
	{
		m_Error.Add(sLog);
		if(m_Error.Count > 20)
			m_Error.RemoveAt(0);
	}

	public static void AddSerMsg(string sLog)
	{
		m_SerTxt.Add(sLog);
	}

	public static void EnableView(bool bEnable)
	{
		m_Debug = bEnable;
	}

	public static bool PopDeviceLostMessage()
	{
		bool bIsLost = m_IsDeviceLost;
		m_IsDeviceLost = false;
		return bIsLost;
	}

	void OnGUI()
	{
		if (!m_Debug)
			return;
		GUI.color = Color.red;
		GUILayout.Label("FPS:" + currFPS.ToString("f2"));

		if (Application.isPlaying && m_Error.Count > 0)
		{
			GUI.color = Color.red;
			foreach(string s in m_Error)
				GUILayout.Label(s);

			if (Time.frameCount % 1800 == 0)
				m_Error.Clear();
		}
	}

	void HandleLog(string sLog, string sTackTrace, LogType type)
	{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
		if (m_CheckDeviceLost)
		{
			if (sLog.IndexOf("device lost") >= 0 || sTackTrace.IndexOf("device lost") >= 0)
			{
				m_IsDeviceLost = true;
				return;
			}
		}
#endif
#if !UNITY_WEBPLAYER
		if(type != LogType.Warning)
			m_WriteTxt.Add(sLog);
#endif
		if(type == LogType.Error || type == LogType.Exception)
		{
			AddView(sLog);
			AddView(sTackTrace);
		}
	}

	void Awake()
	{
		m_IsDeviceLost = false;
		m_StartTime = System.DateTime.Now;
		Application.RegisterLogCallback(HandleLog);

#if UNITY_EDITOR || UNITY_STANDALONE
		string sSerMsgPath = Application.persistentDataPath + "/" + m_SerMsgPath;
		if (System.IO.File.Exists(sSerMsgPath))
			System.IO.File.Delete(sSerMsgPath);
#endif

#if !UNITY_WEBPLAYER
		if (Application.isMobilePlatform)
		{
#if UNITY_IPHONE && !UNITY_EDITOR
			string[] files = System.IO.Directory.GetFiles(Application.temporaryCachePath);
#else
			string[] files = System.IO.Directory.GetFiles(Application.persistentDataPath);
#endif
			if (files != null)
			{
				List<string> lstLog = new List<string>();
				for (int i = 0; i < files.Length; i++)
				{
					string f = files[i];
					string sFileName = System.IO.Path.GetFileName(f);
					if (sFileName.StartsWith("Log_"))
						lstLog.Add(f);
				}
				if (lstLog.Count > 10)
				{
					for (int i = 0; i < lstLog.Count - 10; i++)
					{
						string f = files[i];
						try
						{
							if (System.IO.File.Exists(f))
								System.IO.File.Delete(f);
						}
						catch (System.Exception e)
						{
							Debug.LogException(e);
						}
					}
				}
			}
		}
#if UNITY_IPHONE && !UNITY_EDITOR
		string sLogpath = Application.temporaryCachePath + "/" + m_LogPath;
#else
		string sLogpath = Application.persistentDataPath + "/" + m_LogPath;
#endif
		if (System.IO.File.Exists(sLogpath))
			System.IO.File.Delete(sLogpath);
#if UNITY_IOS
		iPhone.SetNoBackupFlag(sLogpath);
#endif
		Debug.Log("开始记录日志：" + sLogpath);
#endif
	}

	void Start () {
		lastInterval = Time.realtimeSinceStartup;
		frames = 0;
	}
	
	// Update is called once per frame
	void Update () {
		++frames;
		float timeNow = Time.realtimeSinceStartup;
		if (timeNow > lastInterval + updateInterval)
		{
			currFPS = (float)(frames / (timeNow - lastInterval));
			frames = 0;
			lastInterval = timeNow;
		}

#if !UNITY_WEBPLAYER
		if (m_WriteTxt.Count > 0)
		{
			string[] temp = m_WriteTxt.ToArray();
#if UNITY_IPHONE && !UNITY_EDITOR
			string sLogpath = Application.temporaryCachePath + "/" + m_LogPath;
#else
			string sLogpath = Application.persistentDataPath + "/" + m_LogPath;
#endif
			foreach(string t in temp)
			{
				using(System.IO.StreamWriter writer = new System.IO.StreamWriter(sLogpath, true, Encoding.UTF8))
				{
					writer.WriteLine(t);
				}
				m_WriteTxt.Remove(t);
			}
		}
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
		if (m_SerTxt.Count > 0)
		{
			string[] temp = m_SerTxt.ToArray();
			string sSerMsgPath = Application.persistentDataPath + "/" + m_SerMsgPath;
			foreach(string t in temp)
			{
				using(System.IO.StreamWriter writer = new System.IO.StreamWriter(sSerMsgPath, true, Encoding.UTF8))
				{
					writer.WriteLine(t);
				}
				m_SerTxt.Remove(t);
			}
		}
#endif
	}

	public static void Log(object message)
	{
		if (!m_Debug)
			return;
		Debug.Log(message);
	}

	public static void LogError(object message)
	{
		Debug.LogError(message);
	}

	public static void LogError(object message, Object context)
	{
		Debug.LogError(message, context);
	}

	public static void LogException(System.Exception e)
	{
		Debug.LogException(e);
	}

	public static void LogWarning(object message)
	{
		if (!m_Debug)
			return;
		Debug.LogWarning(message);
	}

	public static void LogWarning(object message, Object context)
	{
		if (!m_Debug)
			return;
		Debug.LogWarning(message, context);
	}

	public static void FlushLog(object message)
	{
		if (message == null)
			return;
#if UNITY_IPHONE && !UNITY_EDITOR
		string sLogpath = Application.temporaryCachePath + "/" + m_FlushLogPath;
#else
		string sLogpath = Application.persistentDataPath + "/" + m_FlushLogPath;
#endif
		using(System.IO.StreamWriter writer = new System.IO.StreamWriter(sLogpath, true, Encoding.UTF8))
		{
			writer.WriteLine(message.ToString());
		}
	}
}
