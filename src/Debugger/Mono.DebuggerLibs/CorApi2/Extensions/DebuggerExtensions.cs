//
// DebuggerExtensions.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Samples.Debugging.Extensions
{
	[CLSCompliant(false)]
	public static class DebuggerExtensions
	{
		// [Xamarin] Output redirection.
		[DllImport("kernel32.dll")]
		public static extern bool ReadFile(SafeFileHandle hFile, byte[] lpBuffer, int nNumberOfBytesToRead, out int lpNumberOfBytesRead, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);

		[DllImport("kernel32.dll")]
		public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, SafeFileHandle hSourceHandle, IntPtr hTargetProcessHandle, out SafeFileHandle lpTargetHandle, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

		[DllImport("kernel32.dll")]
		public static extern SafeFileHandle GetStdHandle(uint nStdHandle);

		static void CreateHandles(STARTUPINFOW si, out SafeFileHandle outReadPipe, out SafeFileHandle errorReadPipe)
		{
			si.dwFlags |= STARTF.STARTF_USESTDHANDLES;
			SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES = new SECURITY_ATTRIBUTES();
			sECURITY_ATTRIBUTES.bInheritHandle = true;
			IntPtr currentProcess = GetCurrentProcess();
			if (!CreatePipe(out var hReadPipe, out var hWritePipe, sECURITY_ATTRIBUTES, 0))
			{
				throw new Exception("Pipe creation failed");
			}
			if (!CreatePipe(out var hReadPipe2, out var hWritePipe2, sECURITY_ATTRIBUTES, 0))
			{
				throw new Exception("Pipe creation failed");
			}
			if (!DuplicateHandle(currentProcess, hReadPipe, currentProcess, out outReadPipe, 0u, bInheritHandle: false, 2u))
			{
				throw new Exception("Pipe creation failed");
			}
			if (!DuplicateHandle(currentProcess, hReadPipe2, currentProcess, out errorReadPipe, 0u, bInheritHandle: false, 2u))
			{
				throw new Exception("Pipe creation failed");
			}
			NativeMethods.CloseHandle(currentProcess);
			hReadPipe.Close();
			hReadPipe2.Close();
			si.hStdInput = GetStdHandle(4294967286u).DangerousGetHandle();
			si.hStdOutput = hWritePipe.DangerousGetHandle();
			si.hStdError = hWritePipe2.DangerousGetHandle();
		}

		public static void SetupOutputRedirection(STARTUPINFOW si, ref CreateProcessFlags flags, out SafeFileHandle outReadPipe, out SafeFileHandle errorReadPipe)
		{
			if ((flags & CreateProcessFlags.PROFILE_SERVER) != 0)
			{
				CreateHandles(si, out outReadPipe, out errorReadPipe);
				flags = (CreateProcessFlags)((int)flags & -1073741825);
				return;
			}
			outReadPipe = null;
			errorReadPipe = null;
			si.hStdInput = IntPtr.Zero;
			si.hStdOutput = IntPtr.Zero;
			si.hStdError = IntPtr.Zero;
		}

		public static void TearDownOutputRedirection(SafeFileHandle outReadPipe, SafeFileHandle errorReadPipe, STARTUPINFOW si, CorProcess ret)
		{
			if (outReadPipe != null)
			{
				//si.hStdInput.Close();
				//si.hStdOutput.Close();
				//si.hStdError.Close();
				ret.TrackStdOutput(outReadPipe, errorReadPipe);
			}
		}

		public static IntPtr SetupEnvironment(IDictionary<string, string> environment)
		{
			if (environment == null)
				return IntPtr.Zero;

			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach (KeyValuePair<string, string> item in environment)
				sb.Append(item.Key).Append("=").Append(item.Value).Append('\0');
			string text = sb.Append('\0').ToString();
			return Marshal.StringToHGlobalAnsi(text);
		}

		public static void TearDownEnvironment(IntPtr env)
		{
			if (env != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(env);
			}
		}
	}
}