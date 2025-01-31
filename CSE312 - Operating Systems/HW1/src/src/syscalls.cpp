
#include <syscalls.h>
 
using namespace myos;
using namespace myos::common;
using namespace myos::hardwarecommunication;
 
void printf(char*);

// System call enumeration
enum Syscalls {
    EXIT,
    GETPID,
    WAITPID,
    FORK,
    EXEC,
    PRINTF,
};

// Get process ID
int myos::getpid() {
    int pid = -1;
    asm("int $0x80" : "=c" (pid) : "a"(Syscalls::GETPID));
    return pid;
}

// Wait for a specific process ID
int myos::waitpid(common::uint8_t pid) {
    int result;
    asm("int $0x80" : "=c"(result) : "a"(Syscalls::WAITPID), "b"(pid));
    return result;
}

void myos::exit() {
    asm("int $0x80" : : "a"(Syscalls::EXIT));
}

void myos::syscall_printf(char* str) {
    asm("int $0x80" : : "a"(Syscalls::PRINTF), "b"(str));
}

int myos::fork(int *pid) {
    asm("int $0x80" : "=c" (*pid): "a"(Syscalls::FORK));
    return *pid;
}

int myos::exec(void entrypoint()) {
    int result;
    asm("int $0x80" : "=c"(result) : "a"(Syscalls::EXEC), "b"((uint32_t)entrypoint));
    return result;
}

SyscallHandler::SyscallHandler(InterruptManager* interruptManager, uint8_t InterruptNumber)
:    InterruptHandler(interruptManager, InterruptNumber  + interruptManager->HardwareInterruptOffset())
{
}

SyscallHandler::~SyscallHandler()
{
}


uint32_t SyscallHandler::HandleInterrupt(uint32_t esp)
{
    CPUState* cpu = (CPUState*)esp;

    switch(cpu->eax)
    {
        case Syscalls::PRINTF:
            printf((char*)cpu->ebx);
            break;

        case Syscalls::EXEC: 
            //esp = InterruptHandler::exec(cpu->ebx);
            break;    

        case Syscalls::FORK: 
            cpu->ecx = InterruptHandler::fork(cpu);
            return InterruptHandler::HandleInterrupt(esp);
            break;    

        case Syscalls::EXIT: 
           if(InterruptHandler::exit()) {
              return InterruptHandler::Reschedule(esp);
           }
            break;    

        case Syscalls::GETPID: 
            //cpu->ecx = InterruptHandler::getpid();
            break; 

        case Syscalls::WAITPID: 
            if(InterruptHandler::waitpid(esp)) {
                return InterruptHandler::Reschedule(esp);
            }
            break;    

        default:
            break;
    }
    return esp;
}

