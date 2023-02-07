#include "RGBdriver.h"
#include "superPacket.cpp"

#define CLK 3//pins definitions for the driver        
#define DIO 2
#define updateLedTime 10// temps a canviar el color dels leds (Atmega168 5v)

String textdeSerial = String();
RGBdriver Driver(CLK, DIO);
superPacket parsejador = superPacket();
char charBuffer[(parsejador.numBytesPacket * 2) + 1];// per passar a hex

struct colorsRGB {
	int r;
	int g;
	int b;
};
struct diffRGB {
	float r;
	float g;
	float b;
};

colorsRGB rgb_old;
colorsRGB rgb_LastStep;
diffRGB rgb_step;

int nSteps;
unsigned long tempsOrdreAnterior = 0;
unsigned long tempsEntreOrdres;

void setup()
{
	Serial.begin(57600);
	Serial.println("#Boot ok");
}
void loop()
{
	if (Serial.available() > 0) {
		textdeSerial = Serial.readStringUntil('\n');
		textdeSerial.toUpperCase();

		if (((textdeSerial.substring(0, 2)).equals("0X")) && (textdeSerial.length() == (2 + (parsejador.numBytesPacket * 2)))) {
			tempsEntreOrdres = millis() - tempsOrdreAnterior;
			tempsOrdreAnterior = millis();
			textdeSerial = textdeSerial.substring(2);
			textdeSerial.toCharArray(charBuffer, (parsejador.numBytesPacket * 2) + 1);
			parsejador.parseBufferHex(charBuffer);
			if (parsejador.byteArray[3] != 0) {
				Driver.begin();
				Driver.SetColor(parsejador.byteArray[0],
					parsejador.byteArray[1],
					parsejador.byteArray[2]
				);
				Driver.end();
			}
			else {
				canviSuauA(parsejador.byteArray[0],
					parsejador.byteArray[1],
					parsejador.byteArray[2],
					parsejador.byteArray[3]
				);
			}
			
		}
		else
		{
			Serial.println("#Parse error");
		}
	}
}

void canviSuauA(uint8_t r, uint8_t g, uint8_t b, uint8_t extraWait) {

	nSteps = int(tempsEntreOrdres / (updateLedTime + extraWait)) - 1;

	rgb_step.r = (float)(r-rgb_old.r) / (float)nSteps;
	rgb_step.g = (float)(g-rgb_old.g) / (float)nSteps;
	rgb_step.b = (float)(b-rgb_old.b) / (float)nSteps;

	for (float i = 0; i < nSteps; i++) {

		rgb_step.r = rgb_old.r + (int)(rgb_step.r * i);
		rgb_step.g = rgb_old.g + (int)(rgb_step.g * i);
		rgb_step.b = rgb_old.b + (int)(rgb_step.b * i);

		if ((rgb_LastStep.r == rgb_step.r) && (rgb_LastStep.g == rgb_step.g) && (rgb_LastStep.b == rgb_step.b)) {
			Driver.begin();
			Driver.SetColor(rgb_step.r, rgb_step.g, rgb_step.b);
			Driver.end();
		}
		else {
			delay(updateLedTime - 1);
		}

		rgb_LastStep.r = rgb_step.r;
		rgb_LastStep.g = rgb_step.g;
		rgb_LastStep.b = rgb_step.b;
	}

	rgb_old.r = r;
	rgb_old.g = g;
	rgb_old.b = b;
	
}
