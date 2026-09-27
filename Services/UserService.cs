using AutoMapper;
using InsuranceAPI.DTOs;
using InsuranceAPI.Helpers;
using InsuranceAPI.Models;
using InsuranceAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
        {
            // Email duplicate check
            var existingUser = await _userRepository.GetByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var user = _mapper.Map<User>(createUserDto);
            
            // Hash the password
            user.PasswordHash = PasswordHasher.HashPassword(createUserDto.Password);
            user.CreatedAt = DateTime.UtcNow;

            var createdUser = await _userRepository.CreateAsync(user);
            return _mapper.Map<UserDto>(createdUser);
        }

        public async Task<bool> UpdateAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            // Check email uniqueness if email is modified
            if (user.Email != updateUserDto.Email)
            {
                var existingUser = await _userRepository.GetByEmailAsync(updateUserDto.Email);
                if (existingUser != null)
                {
                    throw new InvalidOperationException("Email is already taken by another user.");
                }
            }

            _mapper.Map(updateUserDto, user);
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return true;
        }
    }
}
