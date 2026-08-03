#include "StdAfx.h"
#include "HookLog.h"
#include <iostream>
#include <windows.h>


// Resolve portable data relative to this injected DLL, not the host process.
std::wstring GetIagdFolder() {
    HMODULE module = nullptr;
    const auto address = reinterpret_cast<LPCWSTR>(&GetIagdFolder);
    const auto flags = GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
        GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT;

    if (!GetModuleHandleExW(flags, address, &module)) {
        OutputDebugStringW(L"Item Assistant could not locate the hook module.\n");
        return std::wstring();
    }

    wchar_t modulePath[MAX_PATH];
    const DWORD length = GetModuleFileNameW(module, modulePath, MAX_PATH);
    if (length == 0 || length >= MAX_PATH) {
        OutputDebugStringW(L"Item Assistant could not resolve the hook module path.\n");
        return std::wstring();
    }

    std::wstring path(modulePath, length);
    const auto separator = path.find_last_of(L"\\/");
    if (separator == std::wstring::npos) {
        OutputDebugStringW(L"Item Assistant received an invalid hook module path.\n");
        return std::wstring();
    }

    const std::wstring userData = path.substr(0, separator + 1) + L"UserData";
    if (!CreateDirectoryW(userData.c_str(), nullptr) && GetLastError() != ERROR_ALREADY_EXISTS) {
        OutputDebugStringW(L"Item Assistant could not create the portable data directory.\n");
        return std::wstring();
    }

    return userData + L"\\";
}

HookLog::HookLog() : m_lastMessageCount(0), m_initialized(false) {
    std::wstring iagdFolder = GetIagdFolder(); // <hook directory>\UserData

    wchar_t tmpfolder[MAX_PATH]; // "%appdata%\..\local\temp\"
    GetTempPath(MAX_PATH, tmpfolder);

    std::wstring logFile(!iagdFolder.empty() ? iagdFolder : tmpfolder);
    logFile += L"iagd_hook.log"; 

    m_out.open(logFile);

    if (m_out.is_open()) {
        m_out
            << L"****************************"  << std::endl
            << L"    Hook Logging Started"      << std::endl
            << L"****************************"  << std::endl;

        TCHAR buffer[MAX_PATH];
        DWORD size = GetCurrentDirectory(MAX_PATH, buffer);
        buffer[size] = '\0';

        m_out << L"Current Directory: " << buffer << std::endl;
    }
}


HookLog::~HookLog() {
    if (m_out.is_open()) {
		writeRepeatSummary();
        m_out
            << L"****************************" << std::endl
            << L"   Hook Logging Terminated  " << std::endl
            << L"****************************" << std::endl;

        m_out.close();
    }
}

void HookLog::out(const char* src, bool forceFlush) {
	return out(std::wstring(src, src + strlen(src)), forceFlush);
}

/// Emit the "repeated N times" line for the message we are about to move off of.
/// Callers must already hold m_mutex.
void HookLog::writeRepeatSummary() {
	if (m_lastMessageCount > 1) {
		m_out << L"    (last message repeated " << m_lastMessageCount << L" times)" << std::endl;
	}
}

void HookLog::out( std::wstring const& output, bool forceFlush ) {
	std::lock_guard<std::mutex> guard(m_mutex);

    if (m_out.is_open()) {
        if (!m_lastMessage.empty()) {
            if (m_lastMessage.compare(output) == 0) {
                ++m_lastMessageCount;
            }
            else {
				writeRepeatSummary();
                m_lastMessage = output;
                m_lastMessageCount = 1;
                m_out << output.c_str() << std::endl;
            }
        }
        else {
            m_lastMessage = output;
            m_lastMessageCount = 1;
            m_out << output.c_str() << std::endl;
        }

		if (!m_initialized || forceFlush) {
			m_out.flush();
		}
    }
}

void HookLog::setInitialized(bool b) {
	m_initialized = b;
}
