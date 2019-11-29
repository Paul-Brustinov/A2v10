// Copyright � 2008-2017 Alex Kukhtin. All rights reserved.

#pragma once

#undef AFX_DATA
#define AFX_DATA AFX_BASE_DATA

class CA2WinApp : public CWinAppEx
{
public:
	CA2WinApp();
	virtual ~CA2WinApp() override;

	virtual BOOL InitInstance() override;
	virtual int ExitInstance() override;

protected:
	void LoadLangLibrary();

	virtual void PreLoadState() override;
	virtual void LoadCustomState() override;
	virtual void SaveCustomState() override;

	DECLARE_MESSAGE_MAP()

	afx_msg void OnAppAbout();
	afx_msg void OnFileSaveAll();
	afx_msg void OnCloseAllDocuments();
	afx_msg void OnUpdateCloseAllDocuments(CCmdUI* pCmdUI);
	afx_msg void OnToolsOptions();
	afx_msg void OnAppLicense();
};

class CA2CommandLineInfo : public CCommandLineInfo
{
	bool m_bDebug;
public:
	CA2CommandLineInfo()
		: m_bDebug(false) {}
	virtual void ParseParam(const TCHAR* pszParam, BOOL bFlag, BOOL bLast);
	bool IsDebugMode() const
		{ return m_bDebug; }
};

#undef AFX_DATA
#define AFX_DATA

