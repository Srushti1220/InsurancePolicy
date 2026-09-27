using AutoMapper;
using InsuranceAPI.DTOs;
using InsuranceAPI.Models;
using InsuranceAPI.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsuranceAPI.Services
{
    public class PolicyTypeService : IPolicyTypeService
    {
        private readonly IPolicyTypeRepository _policyTypeRepository;
        private readonly IMapper _mapper;

        public PolicyTypeService(IPolicyTypeRepository policyTypeRepository, IMapper mapper)
        {
            _policyTypeRepository = policyTypeRepository;
            _mapper = mapper;
        }

        public async Task<PolicyTypeDto?> GetByIdAsync(int id)
        {
            var policyType = await _policyTypeRepository.GetByIdAsync(id);
            return policyType == null ? null : _mapper.Map<PolicyTypeDto>(policyType);
        }

        public async Task<IEnumerable<PolicyTypeDto>> GetAllAsync()
        {
            var policyTypes = await _policyTypeRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<PolicyTypeDto>>(policyTypes);
        }

        public async Task<PolicyTypeDto> CreateAsync(CreatePolicyTypeDto createPolicyTypeDto)
        {
            var policyType = _mapper.Map<PolicyType>(createPolicyTypeDto);
            var created = await _policyTypeRepository.CreateAsync(policyType);
            return _mapper.Map<PolicyTypeDto>(created);
        }

        public async Task<bool> UpdateAsync(int id, UpdatePolicyTypeDto updatePolicyTypeDto)
        {
            var policyType = await _policyTypeRepository.GetByIdAsync(id);
            if (policyType == null) return false;

            _mapper.Map(updatePolicyTypeDto, policyType);
            await _policyTypeRepository.UpdateAsync(policyType);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var policyType = await _policyTypeRepository.GetByIdAsync(id);
            if (policyType == null) return false;

            await _policyTypeRepository.DeleteAsync(id);
            return true;
        }
    }
}
