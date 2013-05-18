/*
 * IRremote: IRrecvDemo - demonstrates receiving IR codes with IRrecv
 * An IR detector/demodulator must be connected to the input RECV_PIN.
 * Version 0.1 July, 2009
 * Copyright 2009 Ken Shirriff
 * http://arcfn.com
 */

#include <IRremote.h>
#include <LazerTagIr.h>

#define LAZERTAG_RELAY_VERSION "1.0.0"
#define LAZERTAG_RELAY_VERSION_FULL "LazerTagRelay Version " LAZERTAG_RELAY_VERSION " (" __DATE__ " " __TIME__ ")"

int RECEIVE_PIN = 11;

LazerTagIrReceive lazerTagReceive(RECEIVE_PIN);
LazerTagIrSend lazerTagSend;
decode_results results;

void setup();
void loop();
void dump(decode_results *results);

void setup()
{
  Serial.begin(115200);
  Serial.println(LAZERTAG_RELAY_VERSION_FULL);
  Serial.println(LAZERTAG_IR_VERSION_FULL);
  Serial.println("Start");
  lazerTagReceive.enableIRIn();
}

void loop()
{
  if (lazerTagReceive.decode(&results))
  {
    dump(&results);
    lazerTagReceive.enableIRIn();
    lazerTagReceive.resume();
  }
  else if (Serial.available() >= 2)
  {
    byte high = Serial.read();
    byte low = Serial.read();
    
    short value = (short)low | (((short)high & 0x1) << 8);
    byte count = (high >> 1) & 0xf;
    bool beacon = (high >> 5) & 0x1;

    lazerTagSend.sendLazerTag(value, count, beacon);
    
    lazerTagReceive.enableIRIn();
    lazerTagReceive.resume();
  }
}

void dump(decode_results *results)
{
  switch (results->decode_type)
  {
    case TYPE_LAZERTAG_TAG:
      Serial.print("LTX: ");
      break;
    case TYPE_LAZERTAG_BEACON:
      Serial.print("LTTO: ");
      break;
    default:
      Serial.print("RAW: ");
      for (int i = 0; i < results->rawlen; i++)
      {
        Serial.print(results->rawbuf[i] * USECPERTICK, DEC);
        if (i < (results->rawlen - 1))
        {
          Serial.print(", ");
        }
      }
      Serial.println();
      return;
  }
  Serial.print(results->value, HEX);
  Serial.print(", ");
  Serial.print(results->bits, DEC);
  Serial.println();
}

