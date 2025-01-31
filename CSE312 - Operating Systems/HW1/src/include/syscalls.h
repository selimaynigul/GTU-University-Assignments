 
#ifndef __MYOS__SYSCALLS_H
#define __MYOS__SYSCALLS_H

#include <common/types.h>
#include <hardwarecommunication/interrupts.h>
#include <multitasking.h>

namespace myos
{
    
    class SyscallHandler : public hardwarecommunication::InterruptHandler
    {
            
        public:
            SyscallHandler(hardwarecommunication::InterruptManager* interruptManager, myos::common::uint8_t InterruptNumber);
            ~SyscallHandler();
            
            virtual myos::common::uint32_t HandleInterrupt(myos::common::uint32_t esp);

    };

    int getpid();
    int waitpid(common::uint8_t pid);
    void exit();
    void syscall_printf(char* str);
    int fork(int* pid);
    int exec(void entrypoint());    
}


#endif