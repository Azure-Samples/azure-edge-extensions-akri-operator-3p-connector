using System.Runtime.InteropServices;

namespace TcpConnector.Accessor;

/// <summary>
/// Wrapper class to connect to Siemens PLC via TCP
/// </summary>
public class TcpAdapter
{
	/// <summary>
	/// Invoke a function on the tcpserver. These functions are normally called via tcpadmin.
	/// This method allows to call them directly.
	/// Example: Send a message to a node by specifying its node id, 's' as the function parameter, and buf/lbuf defining a string buffer and its length respectively.
	/// See tcpkm.h
	/// </summary>
	/// <param name="node"></param>
	/// <param name="fkt"></param>
	/// <param name="buf"></param>
	/// <param name="lbuf"></param>
	/// <returns></returns>
	[DllImport("my_plugin")]
	public static extern IntPtr TCPInvoke(short node, char fkt, IntPtr buf, int lbuf);

	/// <summary>
	/// Wrapper function to call imported TCPInvoke(short node, char fkt, IntPtr buf, int lbuf)
	/// </summary>
	/// <param name="node"></param>
	/// <param name="fkt"></param>
	/// <param name="buffer"></param>
	/// <returns></returns>
	public static string TcpInvoke(short node, char fkt, string buffer)
	{
		int lbuf = buffer.Length;

		IntPtr bufPtr = Marshal.StringToHGlobalAnsi(buffer);
		IntPtr resultPtr = TCPInvoke(node, fkt, bufPtr, lbuf);

		var result = Marshal.PtrToStringAnsi(resultPtr);

		Marshal.FreeHGlobal(bufPtr);

		return result!;
	}

	/// <summary>
	/// Get the status of a node. This is a numeric value representing various states in which a node can be, e.g. TCP_CONNECT.
	/// See tcpkm.h
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	[DllImport("my_plugin")]
	public static extern int TCPStatus(short node);

	/// <summary>
	/// Check if a node is connected
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	[DllImport("my_plugin")]
	public static extern bool TCPIsConnected(short node);

	/// <summary>
	/// Reload the tcp_config file.
	/// </summary>
	[DllImport("my_plugin")]
	public static extern void TCPReloadTCPConfig();

	/// <summary>
	/// Registers a callback function which is called whenever a new message is received from a device.
	/// </summary>
	/// <param name="receiveCallback"></param>
	[DllImport("my_plugin", CallingConvention = CallingConvention.Cdecl)]
	public static extern void TCPSetReceiveCallback(Action<IntPtr> receiveCallback);

	/// <summary>
	/// Initializes the server with a given provider number (number_server).
	/// The provider number must match the provider number specified in the tcp_config file.
	/// </summary>
	/// <param name="number_server"></param>
	/// <returns></returns>
	[DllImport("my_plugin")]
	public static extern int TCPServerInitialize(int number_server);

	/// <summary>
	/// Start the Server, i.e. connect to clients specified in the tcp_config file and allow sending and receving messages.
	/// This is a blocking call.
	/// </summary>
	[DllImport("my_plugin")]
	public static extern void TCPStart();

	/// <summary>
	/// Stop the Server gracefully.
	/// </summary>
	[DllImport("my_plugin")]
	public static extern void TCPStop();

	/// <summary>
	/// Pause the main event loop.
	/// </summary>
	[DllImport("my_plugin")]
	public static extern void TCPPause();

	/// <summary>
	/// tcpcfg.c
	/// </summary>
	/// <returns></returns>
	[DllImport("my_plugin")]
	public static extern int TCPGetPath();

	/// <summary>
	/// Set the path to a tcp config file.
	/// The path must point to a directory which contains the config at ./cfg/tcp_config.
	/// Example: Config file is at /km/cfg/tcp_config. Call TCPSetPath with "/km"
	/// </summary>
	/// <param name="path"></param>
	[DllImport("my_plugin")]
	public static extern void TCPSetPath(string path);
}

[StructLayout(LayoutKind.Sequential)]
public struct PbVTSend
{
	short node_nr; /* V-Par      Knoten von dem Daten kommen */
	IntPtr absender; /* optional   Namen von dem Daten kommen */
	IntPtr daten; /* V-Par      Daten, fuer den Verteiler  */
	long ldaten; /* V-Par      Laenge Felde daten */
	long ret_code; /* R-Par      KM-Errorcode */
	IntPtr ret_text; /* R-Par      Returntext */
}