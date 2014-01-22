#include <stm32f4xx_gpio.h>
#include <stdlib.h>
#include "usart.h"
#include "oled.h"
#include "remote.h"
#include "i2c.h"

#define CMD_NOP					0x00
#define CMD_OLED_SEND_FLUSH		0xDE
#define CMD_OLED_POWER			0xA9
#define CMD_OLED_BRIGHTNESS		0xBA

#define CMD_IR_BUTTON			0xDA

#define CMD_I2C_WRITE			0x9A
#define CMD_I2C_READ			0x9B

volatile uint8_t packetReceived = 0, packetCmd;
volatile uint8_t *packetData;
volatile int packetLength;

volatile uint8_t irData[2];
volatile uint8_t irButtonPressed = 0;

int main(void) 
{
	uint8_t i2cReadBuffer[255];
	uint8_t i2cToRead;

	oledInit();
	usartInit(460800);
	remoteInit();
	i2cInit();

	while (1)
	{
		if (packetReceived)
		{
			switch (packetCmd)
			{
				case CMD_OLED_SEND_FLUSH:
					oledFlush((const uint8_t*)packetData);
					usartSendPacket(packetCmd, NULL, 0);
					break;
		
				case CMD_OLED_POWER:
					oledTurnDisplay(packetData[0]);
					usartSendPacket(packetCmd, NULL, 0);
					break;
		
				case CMD_OLED_BRIGHTNESS:
					oledSetBrightness(packetData[0]);
					usartSendPacket(packetCmd, NULL, 0);
					break;

				case CMD_I2C_WRITE:
					i2cWrite(packetData[0], (uint8_t*)&packetData[1], packetLength - 1);
					usartSendPacket(packetCmd, NULL, 0);
					break;

				case CMD_I2C_READ:
					i2cToRead = packetData[1];
					i2cRead(packetData[0], i2cReadBuffer, i2cToRead);
					usartSendPacket(packetCmd, i2cReadBuffer, i2cToRead);
					break;
			}

			packetReceived = 0;
		}
		
		if (irButtonPressed)
		{
			usartSendPacket(CMD_IR_BUTTON, (const uint8_t*)irData, 2);
			irButtonPressed = 0;
		}
	}
}

void OnRemoteControlButtonPressed(uint8_t address, uint8_t button)
{
	irData[0] = address;
	irData[1] = button;
	irButtonPressed = 1;
}

void ProcessPacket(uint8_t cmd, uint8_t *data, int length)
{
	packetCmd = cmd;
	packetData = data;
	packetLength = length;
	packetReceived = 1;
}
