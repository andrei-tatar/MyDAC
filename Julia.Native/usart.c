#include <stm32f4xx_usart.h>
#include "usart.h"

#define BUFFER_SIZE 1200
volatile uint8_t buffer[BUFFER_SIZE];

const uint8_t packetStart[] = { 0x91, 0xEB, 0xFA, 0x58 };

#define STATE_IDLE			0x01
#define STATE_LENGTH_LSB	0x02
#define STATE_LENGTH_MSB	0x03
#define STATE_DATA			0x04
#define STATE_CHECKSUM		0x05

void usartInit(uint32_t baudrate)
{
	GPIO_InitTypeDef GPIO_InitStruct; // this is for the GPIO pins used as TX and RX
	USART_InitTypeDef USART_InitStruct; // this is for the USART3 initilization
	NVIC_InitTypeDef NVIC_InitStructure; // this is used to configure the NVIC (nested vector interrupt controller)

	//enable APB2 peripheral clock for USART3
	RCC_APB1PeriphClockCmd(RCC_APB1Periph_USART3, ENABLE);

	//enable the peripheral clock for the pins used by USART3, PB10 for TX and PB11 for RX
	RCC_AHB1PeriphClockCmd(RCC_AHB1Periph_GPIOB, ENABLE);

	//set up the TX and RX pins
	GPIO_InitStruct.GPIO_Pin = GPIO_Pin_10 | GPIO_Pin_11; // Pins 10 (TX) and 11 (RX) are used
	GPIO_InitStruct.GPIO_Mode = GPIO_Mode_AF; 			// the pins are configured as alternate function so the USART peripheral has access to them
	GPIO_InitStruct.GPIO_Speed = GPIO_Speed_50MHz;		// this defines the IO speed and has nothing to do with the baudrate!
	GPIO_InitStruct.GPIO_OType = GPIO_OType_PP;			// this defines the output type as push pull mode (as opposed to open drain)
	GPIO_InitStruct.GPIO_PuPd = GPIO_PuPd_UP;			// this activates the pullup resistors on the IO pins
	GPIO_Init(GPIOB, &GPIO_InitStruct);					// now all the values are passed to the GPIO_Init() function which sets the GPIO registers

	//The RX and TX pins are now connected to their AF so that the USART3 can take over control of the pins
	GPIO_PinAFConfig(GPIOB, GPIO_PinSource10, GPIO_AF_USART3); //
	GPIO_PinAFConfig(GPIOB, GPIO_PinSource11, GPIO_AF_USART3);

	USART_StructInit(&USART_InitStruct);
	USART_InitStruct.USART_BaudRate = baudrate;				// the baudrate is set to the value we passed into this init function
	USART_Init(USART3, &USART_InitStruct);					// again all the properties are passed to the USART_Init function which takes care of all the bit setting

	//enable the USART3 receive interrupt
	USART_ITConfig(USART3, USART_IT_RXNE, ENABLE); // enable the USART3 receive interrupt

	NVIC_InitStructure.NVIC_IRQChannel = USART3_IRQn;		 // we want to configure the USART3 interrupts
	NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 0;// this sets the priority group of the USART3 interrupts
	NVIC_InitStructure.NVIC_IRQChannelSubPriority = 0;		 // this sets the subpriority inside the group
	NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;			 // the USART3 interrupts are globally enabled
	NVIC_Init(&NVIC_InitStructure);							 // the properties are passed to the NVIC_Init function which takes care of the low level stuff

	// finally this enables the complete USART3 peripheral
	USART_Cmd(USART3, ENABLE);
}

// this is the interrupt request handler (IRQ) for ALL USART3 interrupts
void USART3_IRQHandler(void)
{
	// check if the USART3 receive interrupt flag was set
	static uint8_t state = STATE_IDLE;
	static uint8_t headerOffset = 0, checksum;
	static int length, dataOffset;
	uint8_t cByte;

	if (USART_GetITStatus(USART3, USART_IT_RXNE) == RESET) return;

	cByte = USART3->DR;
	
	if (cByte == packetStart[headerOffset])
	{
		if (++headerOffset == 4)
		{
			state = STATE_LENGTH_LSB;
			checksum = 0;
			headerOffset = 0;
			return;
		}
	}
	else
		headerOffset = 0;
	
	switch (state)
	{
		case STATE_IDLE:
			break;

		case STATE_LENGTH_LSB:
			length = cByte;
			checksum ^= cByte;
			state = STATE_LENGTH_MSB;
			break;

		case STATE_LENGTH_MSB:
			length |= cByte << 8;
			if (length > BUFFER_SIZE)
			{
				state = STATE_IDLE;
				headerOffset = 0;
			}

			checksum ^= cByte;
			state = STATE_DATA;
			dataOffset = 0;
			break;

		case STATE_DATA:
			buffer[dataOffset++] = cByte;
			checksum ^= cByte;
			if (dataOffset == length)
				state = STATE_CHECKSUM;
			break;

		case STATE_CHECKSUM:
			if (cByte == checksum)
			{
				ProcessPacket(buffer[0], (uint8_t*)&buffer[1], length - 1);
			}
			state = STATE_IDLE;
			headerOffset = 0;
			break;
	}
	USART_ClearITPendingBit(USART3, USART_IT_RXNE);
}

static uint8_t usartSendByte(uint8_t data)
{
	// wait until data register is empty
	while( !(USART3->SR & 0x00000040) );

	USART_SendData(USART3, data);
	return data;
}

static uint8_t usartSendData(uint8_t checksum, const uint8_t *data, int length)
{
	while (length--)
		checksum ^= usartSendByte(*data++);
	return checksum;
}

void usartSendPacket(uint8_t cmd, const uint8_t *data, int length)
{
	uint8_t checksum = 0;

	length += 1;

	usartSendData(0, packetStart, 4);
	checksum ^= usartSendByte(length & 0xFF);
	checksum ^= usartSendByte((length >> 8) & 0xFF);
	checksum ^= usartSendByte(cmd);
	checksum = usartSendData(checksum, data, length - 1);
	usartSendByte(checksum);
}
