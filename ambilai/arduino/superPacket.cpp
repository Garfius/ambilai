#include "Arduino.h"
#include <EEPROM.h>

#ifndef numBytesSuperPacket
#define numBytesSuperPacket 4
#endif

#ifndef _superPacket_H_INCLUDED
#define _superPacket_H_INCLUDED

class superPacket {
public:
	//-------------variables
	static const int numBytesPacket = numBytesSuperPacket;
	byte byteArray[numBytesSuperPacket];
	//-------variables temporals
	char charTmp[2];
	uint8_t c, h;
	void parseBufferHex(char cadena[]) {
		for (int i = 0; i <= (numBytesSuperPacket - 1); i++)
		{
			charTmp[0] = cadena[i * 2];
			charTmp[1] = cadena[(i * 2) + 1];
			this->byteArray[i] = hex8(charTmp);
		}
	}
	int hex8(char *in)
	{
		c = in[0];

		if (c <= '9' && c >= '0') { c -= '0'; }
		else if (c <= 'f' && c >= 'a') { c -= ('a' - 0x0a); }
		else if (c <= 'F' && c >= 'A') { c -= ('A' - 0x0a); }
		else return(-1);

		h = c;

		c = in[1];

		if (c <= '9' && c >= '0') { c -= '0'; }
		else if (c <= 'f' && c >= 'a') { c -= ('a' - 0x0a); }
		else if (c <= 'F' && c >= 'A') { c -= ('A' - 0x0a); }
		else return(-1);

		return (h << 4 | c);
	}
};
#endif