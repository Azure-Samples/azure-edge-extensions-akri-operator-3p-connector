#include "vtif.h"
 
/* tcpadapter.c */
/*
*  TCPInvoke
*  ----------
*  Invoke a function on the tcpserver. These functions are normally called via tcpadmin.
*  This method allows to call them directly.
*  Example: Send a message to a node by specifying its node id, 's' as the function parameter, and buf/lbuf defining a string buffer and its length respectively.
*  See tcpkm.h
*
*/
char* TCPInvoke(short node, char fkt, char *buf, int lbuf);
 
/*
*  TCPStatus
*  ----------
*  Get the status of a node. This is a numeric value representing various states in which a node can be, e.g. TCP_CONNECT.
*  See tcpkm.h
*
*/
int TCPStatus(short node);
 
/*
*  TCPIsConnected
*  ----------
*  
*
*/
bool TCPIsConnected(short node);
 
/*
*  TCPReloadTCPConfig
*  ----------
*  Reload the tcp_config file.
*
*/
void TCPReloadTCPConfig();
 
/*
*  TCPSetReceiveCallback
*  ----------
*  Registers a callback function which is called whenever a new message is received from a device.
*
*/
void TCPSetReceiveCallback(void (*callback)(PbVTSend *));
 
/* tcpserver.c */
/*
*  TCPServerInitialize
*  ----------
*  Initializes the server with a given provider number (number_server).
*  The provider number must match the provider number specified in the tcp_config file.
*
*/
int TCPServerInitialize(int number_server);
 
/*
*  TCPStart
*  ----------
*  Start the Server, i.e. connect to clients specified in the tcp_config file and allow sending and receving messages.
*  This is a blocking call.
*
*/
void TCPStart();
 
/*
*  TCPStop
*  ----------
*  Stop the Server gracefully.
*
*/
void TCPStop();
 
/*
*  TCPPause
*  ----------
*  Pause the main event loop.
*
*/
void TCPPause();
 
/* tcpcfg.c */
int TCPGetPath ();
 
/*
*  TCPSetPath
*  ----------
*  Set the path to a tcp config file.
*  The path must point to a directory which contains the config at ./cfg/tcp_config.
*  Example: Config file is at /km/cfg/tcp_config. Call TCPSetPath with "/km"
*
*/
void TCPSetPath (const char *path);