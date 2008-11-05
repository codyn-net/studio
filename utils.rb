require 'serialize'
require 'components/attachment'

class Object
	def call_super(func, *args)
		call_super_n(0, func, *args)
	end
	
	def call_super_n(n, func, *args)
		self.class.ancestors[(n + 1)..-1].each do |x|
			if x.instance_methods.include?(func.to_s)
				return x.instance_method(func).bind(self).call(*args)
			end
		end
	end
end

def String
	def numeric?
		self =~ /^[0-9.\-\s]*$/
	end
end

module ArrayOperations
	class ResampleException < Exception
	end
	
	def *(v)
		collect { |x| x * v }
	end
	
	def /(v)
		collect { |x| x / v }
	end
	
	def bsearch(val)
		left = 0
		right = self.length
		
		while right > left
			probe = (left + right) / 2
			
			if self[probe] > val
				right = probe - 1
			elsif self[probe] < val
				left = probe + 1
			else
				return probe
			end
		end
		
		return right + (self[right] && self[right] < val ? 1 : 0)
	end
	
	def resample(sites, to)
		sites = sites.to_a if sites.is_a?(Range)
		to = to.to_a if to.is_a?(Range)

		raise(ResampleException, 'Number of sites do not match number of data points') if sites.length != self.length
		
		data = []
		each_index { |i| data << [self[i].to_f, sites[i]] }
		
		# first sort data according to the sites
		data.sort! { |a,b| a[1] <=> b[1] }
		data.collect! { |x| x[0] }
		sites.sort!
		
		# resample the data being at sites in 'from' to the sites in 'to'
		# using linear interpolation
		result = []
		
		to.each do |res|
			idx = sites.bsearch(res)
			
			fidx = idx > 0 ? idx - 1 : 0
			sidx = idx < data.length ? idx : -1
			
			# interpolate between the values found in data
			factor = sites[sidx] == sites[fidx] ? 1 : (sites[sidx] - res) / (sites[sidx] - sites[fidx]).to_f
			result << data[fidx] * factor + (data[sidx] * (1 - factor))
		end
		
		result
	end
	
	def inject(n)
		each { |value| n = yield(n, value) }
		n
	end

	def sum
		inject(0) { |n, value| n + value }
	end

	def swap(i1, i2)
		tmp = self[i1]
		self[i1] = self[i2]
		self[i2] = tmp
	end
end

class Array
	include ArrayOperations
end

module RadDeg
	def to_rad
		self / 180.0 * Math::PI
	end
	
	def to_deg
		self / Math::PI * 180.0
	end
end

class Fixnum
	include RadDeg
end

class Bignum
	include RadDeg
end

class Float
	include RadDeg

	def frac
		self - self.floor
	end
end

module Cpg
	class Hash < ::Hash
		include Cpg::Serialize::Dynamic
	
		def properties
			k = self.keys
			
			k.sort! {|a,b| a.to_s <=> b.to_s }
			k.delete(:id)
			k
		end
	
		def get_property(name)
			self[name.to_sym]
		end
	
		def set_property(name, val)
			return false if name.to_sym == :id
			self[name.to_sym] = val
		end
	end

	module SortedArray
		def sort_impl(a, b)
			if !a.is_a?(Components::Attachment) && b.is_a?(Components::Attachment)
				-1
			elsif a.is_a?(Components::Attachment) && !b.is_a?(Components::Attachment)
				1
			else
				0
			end
		end
	
		def sort!
			super do |a,b|
				sort_impl(a, b)
			end
		end
	
		def sort
			super do |a,b|
				sort_impl(a, b)
			end
		end
	end

	class Array < ::Array
		include SortedArray
	end
	
	class SerialArray < Array
		include Cpg::Serialize::Dynamic
	
		def properties
			self
		end
	
		def ensure_property(name)
			self << name.to_sym unless self.include?(name.to_sym)
		end
	
		def set_property(name, val)
			ensure_property(name)
		end
	
		def get_property(name)
			ensure_property(name)
			''
		end
	end
end
