#include "../IRremote/IRremote.h"
#include "../IRremote/IRremoteInt.h"

#ifndef LazerTagIr_h
#define LazerTagIr_h

#define LAZERTAG_IR_VERSION "1.0.0"
#define LAZERTAG_IR_VERSION_FULL PSTR("LazerTagIr Library Version " LAZERTAG_IR_VERSION)

#define TYPE_LAZERTAG_TAG 100
#define TYPE_LAZERTAG_BEACON 101

#define LAZERTAG_SIG_PS 3000			// Presync: Active 3ms +/- 10%
#define LAZERTAG_SIG_PSP 6000			// Presync Pause: Inactive 6ms +/- 10%
#define LAZERTAG_SIG_TAG_SYNC 3000		// Sync: Active 3ms +/- 10%
#define LAZERTAG_SIG_BEACON_SYNC 6000	// Sync: Active 6ms +/- 10%
#define LAZERTAG_SIG_BIT_ZERO 1000		// 0: Active 1ms +/- 10%
#define LAZERTAG_SIG_BIT_ONE 2000		// 1: Active 2ms +/- 10%
#define LAZERTAG_SIG_BIT_PAUSE 2000		// Pause: Inactive 2ms +/- 10%
#define LAZERTAG_SIG_SFP 18000			// Special Format Pause: 18ms +/- 10%

class LazerTagIrReceive : public IRrecv
{
public:
	LazerTagIrReceive(int receivePin);
	int decode(decode_results *results);
private:
	long decodeLazerTag(decode_results *results);
};

class LazerTagIrSend : public IRsend
{
public:
	void sendLazerTag(unsigned long data, int nbits, bool beacon);
};

#endif // LazerTagIr_h
