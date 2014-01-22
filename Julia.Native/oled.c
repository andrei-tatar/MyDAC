#include "oled.h"
#include <stm32f4xx_gpio.h>

#define DC_DC_ENABLE		GPIOC->BSRRL = GPIO_Pin_0 //PC0 - DC-DC converter
#define DC_DC_DISABLE		GPIOC->BSRRH = GPIO_Pin_0
#define DC_HIGH				GPIOC->BSRRL = GPIO_Pin_1 //PC1 - D/C
#define DC_LOW				GPIOC->BSRRH = GPIO_Pin_1
#define WR_HIGH				GPIOA->BSRRL = GPIO_Pin_4 //PA4 - WR
#define WR_LOW				GPIOA->BSRRH = GPIO_Pin_4

static void delay_us(volatile uint32_t us)
{
	us *= 42;
	while (us--);
}

static void oledWrite(uint8_t data)
{
	if (data & 0x01) GPIOC->BSRRL = GPIO_Pin_5; else GPIOC->BSRRH = GPIO_Pin_5; //PC5 - D0
	if (data & 0x02) GPIOC->BSRRL = GPIO_Pin_6; else GPIOC->BSRRH = GPIO_Pin_6; //PC6 - D1
	if (data & 0x04) GPIOA->BSRRL = GPIO_Pin_7; else GPIOA->BSRRH = GPIO_Pin_7; //PA7 - D2
	if (data & 0x08) GPIOC->BSRRL = GPIO_Pin_7; else GPIOC->BSRRH = GPIO_Pin_7; //PC7 - D3
	if (data & 0x10) GPIOA->BSRRL = GPIO_Pin_6; else GPIOA->BSRRH = GPIO_Pin_6; //PA6 - D4
	if (data & 0x20) GPIOA->BSRRL = GPIO_Pin_2; else GPIOA->BSRRH = GPIO_Pin_2; //PA2 - D5
	if (data & 0x40) GPIOA->BSRRL = GPIO_Pin_3; else GPIOA->BSRRH = GPIO_Pin_3; //PA3 - D6
	if (data & 0x80) GPIOA->BSRRL = GPIO_Pin_1; else GPIOA->BSRRH = GPIO_Pin_1; //PA1 - D7

	WR_LOW;
	delay_us(1);
	WR_HIGH;
}

static void oledInitPorts()
{
	GPIO_InitTypeDef GPIO_InitStruct;

	RCC_AHB1PeriphClockCmd(RCC_AHB1Periph_GPIOC, ENABLE);
	GPIO_InitStruct.GPIO_Pin = GPIO_Pin_0 | GPIO_Pin_1 | GPIO_Pin_5 | GPIO_Pin_6 | GPIO_Pin_7;
	GPIO_InitStruct.GPIO_Mode = GPIO_Mode_OUT; 		// we want the pins to be an output
	GPIO_InitStruct.GPIO_Speed = GPIO_Speed_50MHz; 	// this sets the GPIO modules clock speed
	GPIO_InitStruct.GPIO_OType = GPIO_OType_PP; 	// this sets the pin type to push / pull (as opposed to open drain)
	GPIO_InitStruct.GPIO_PuPd = GPIO_PuPd_NOPULL; 	// this sets the pullup / pulldown resistors to be inactive
	GPIO_Init(GPIOC, &GPIO_InitStruct); 			// this finally passes all the values to the GPIO_Init function which takes care of setting the corresponding bits.

	RCC_AHB1PeriphClockCmd(RCC_AHB1Periph_GPIOA, ENABLE);
	GPIO_InitStruct.GPIO_Pin = GPIO_Pin_1 | GPIO_Pin_2 | GPIO_Pin_3 | GPIO_Pin_4 | GPIO_Pin_6 | GPIO_Pin_7;
	GPIO_InitStruct.GPIO_Mode = GPIO_Mode_OUT; 		// we want the pins to be an output
	GPIO_InitStruct.GPIO_Speed = GPIO_Speed_50MHz; 	// this sets the GPIO modules clock speed
	GPIO_InitStruct.GPIO_OType = GPIO_OType_PP; 	// this sets the pin type to push / pull (as opposed to open drain)
	GPIO_InitStruct.GPIO_PuPd = GPIO_PuPd_NOPULL; 	// this sets the pullup / pulldown resistors to be inactive
	GPIO_Init(GPIOA, &GPIO_InitStruct);			  	// this passes the configuration to the Init function which takes care of the low level stuff
}

void oledInit()
{
	uint8_t init[] =
    {
        0xAE, //display off

        0xAD, //internal DC-DC off
        0x8A, //2nd

        0xA8, //MUX Ratio
        0x3F, //64 duty

        0xD3, //Display offset
        0x00, //Second byte

        0x40, //Start line
        0xA1, //Segment remap
        0xC8, //COM remap
        0xA6, //Set normal/inverse display (0xA6:Normal display)
        0xA4, //Set entire display on/off (0xA4:Normal display)

        0x81, //Contrast setting
        0x5C, //Second byte

        0xD5, //Frame rate
        0x60, // 85 Hz

        0xD8, //Mode setting
        0x00, //Mono mode

        0xD9, //Set Pre-charge period
        0x84 // Second byte
    };
	int i;

	oledInitPorts();
	WR_HIGH;
	DC_DC_DISABLE;

	DC_LOW;
	for (i=0; i<sizeof(init); i++)
		oledWrite(init[i]);
}

void oledTurnDisplay(uint8_t turnOn)
{
	DC_LOW;
	if (turnOn)
	{
		DC_DC_ENABLE;
		delay_us(50000);
		oledWrite(0xAF);
	}
	else
	{
		oledWrite(0xAE);
		delay_us(50000);
		DC_DC_DISABLE;
	}
}

void oledSetBrightness(uint8_t brightness)
{
	DC_LOW;
	oledWrite(0x81);
	oledWrite(brightness);
}

void oledFlush(const uint8_t *data)
{
	uint8_t x, y;
	for (y = 0; y < 8; y++)
	{
		DC_LOW;
		oledWrite(0xB0 | y);
		oledWrite(0x02);
		oledWrite(0x10);

		DC_HIGH;
		for (x = 0; x < 128; x++)
			oledWrite(*data++);
	}
}
